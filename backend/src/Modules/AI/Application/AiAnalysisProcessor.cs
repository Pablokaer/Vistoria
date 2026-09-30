using System.Text.Json;
using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// Executes one queued analysis: loads photos from storage, calls <see cref="IImageAnalysisService"/>,
/// stores the structured result and applies the text to the room/defect/comparison as an AI suggestion.
/// </summary>
public sealed class AiAnalysisProcessor(
    IAppDbContext db,
    IImageAnalysisService analyzer,
    IStorageService storage,
    ReportSnapshotStore snapshots,
    InspectFlow.Modules.Inspections.Application.InspectionWriteScope writeScope,
    IAuditLogger audit,
    IClock clock,
    ILogger<AiAnalysisProcessor> logger)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(Guid analysisId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        // Claim atomically so a job is never processed twice (e.g. re-queued after restart).
        var claimed = await db.AiAnalyses
            .Where(a => a.Id == analysisId && a.Status == AiAnalysisStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AiAnalysisStatus.Processing)
                .SetProperty(a => a.StartedAt, now).SetProperty(a => a.Attempts, a => a.Attempts + 1), ct);
        if (claimed == 0) return;

        var analysis = await db.AiAnalyses.FirstAsync(a => a.Id == analysisId, ct);
        (string Description, decimal? Confidence, DateTimeOffset At)? applied = null;
        try
        {
            var room = await db.InspectionRooms.AsNoTracking().FirstAsync(r => r.Id == analysis.InspectionRoomId, ct);
            var images = await LoadImagesAsync(analysis.MediaIds, ct);
            // Read at processing time: the queue delay is seconds, and the company setting is the source of truth.
            var companyLanguage = await db.Inspections.Where(i => i.Id == analysis.InspectionId)
                .Join(db.Companies, i => i.CompanyId, c => c.Id, (i, c) => c.ReportLanguage).FirstOrDefaultAsync(ct);
            var language = AiOutputLanguage.For(companyLanguage);
            string description;
            object result;
            decimal? confidence = null;

            switch (analysis.Kind)
            {
                case AiAnalysisKind.RoomDescription:
                {
                    var r = await analyzer.AnalyzeRoomAsync(new RoomAnalysisRequest(room.Name, room.RoomType.ToString(), images, language), ct);
                    description = r.Description;
                    result = r;
                    break;
                }
                case AiAnalysisKind.DefectDescription:
                {
                    var defect = await db.InspectionDefects.AsNoTracking().FirstAsync(d => d.Id == analysis.DefectId, ct);
                    var r = await analyzer.AnalyzeDefectAsync(new DefectAnalysisRequest(room.Name, defect.Description, images, language), ct);
                    description = r.Description;
                    confidence = r.Confidence;
                    result = r;
                    break;
                }
                case AiAnalysisKind.RoomComparison:
                {
                    var r = await CompareAsync(room, images, language, ct);
                    description = r.Summary;
                    confidence = r.Confidence;
                    result = r;
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unknown analysis kind {analysis.Kind}");
            }

            var completedAt = clock.UtcNow;
            analysis.Status = AiAnalysisStatus.Completed;
            analysis.Description = description;
            analysis.Confidence = confidence;
            analysis.ResultJson = JsonSerializer.Serialize(result, result.GetType(), Json);
            analysis.CompletedAt = completedAt;
            analysis.Error = null;
            audit.Record(AuditActions.AIAnalysisCompleted, nameof(AiAnalysis), analysis.Id,
                new { analysis.InspectionId, roomId = analysis.InspectionRoomId, kind = analysis.Kind.ToString(), provider = analysis.Provider },
                analysis.RequestedBy);
            await db.SaveChangesAsync(ct);
            applied = (description, confidence, completedAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "AI analysis {AnalysisId} failed", analysisId);
            analysis.Status = AiAnalysisStatus.Failed;
            analysis.Error = ex is AiProviderException ? ex.Message : AiErrorTexts.Generic.En;
            analysis.CompletedAt = clock.UtcNow;
            audit.Record(AuditActions.AIAnalysisFailed, nameof(AiAnalysis), analysis.Id,
                new { analysis.InspectionId, kind = analysis.Kind.ToString(), provider = analysis.Provider }, analysis.RequestedBy);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        if (applied is { } a)
            await ApplyAsync(analysis, a.Description, a.Confidence, a.At, ct);
    }

    /// <summary>
    /// Applies AI text with targeted updates so a concurrent agent edit is never overwritten:
    /// AI fields are always updated; the final text is pre-filled only when still empty.
    /// Results for finalized inspections are stored but not applied.
    /// </summary>
    private Task ApplyAsync(AiAnalysis analysis, string description, decimal? confidence, DateTimeOffset now, CancellationToken ct) =>
        // Same shared lock as agent edits: an AI result can never land after (or during) finalization.
        writeScope.RunAsync(analysis.InspectionId, async () =>
        {
            await ApplyCoreAsync(analysis, description, confidence, now, ct);
            return true;
        }, ct);

    private async Task ApplyCoreAsync(AiAnalysis analysis, string description, decimal? confidence, DateTimeOffset now, CancellationToken ct)
    {
        var status = await db.Inspections.Where(i => i.Id == analysis.InspectionId).Select(i => i.Status).FirstAsync(ct);
        if (!InspectionStateMachine.IsEditable(status)) return;

        switch (analysis.Kind)
        {
            case AiAnalysisKind.RoomDescription:
                await db.InspectionRooms.Where(r => r.Id == analysis.InspectionRoomId)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.AiDescription, description)
                        .SetProperty(r => r.LatestAiAnalysisId, analysis.Id)
                        .SetProperty(r => r.AiDescriptionGeneratedAt, now)
                        .SetProperty(r => r.UpdatedAt, now), ct);
                await db.InspectionRooms
                    .Where(r => r.Id == analysis.InspectionRoomId && (r.FinalDescription == null || r.FinalDescription == ""))
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.FinalDescription, description)
                        .SetProperty(r => r.FinalDescriptionSource, DescriptionSource.Ai), ct);
                break;
            case AiAnalysisKind.DefectDescription:
                await db.InspectionDefects.Where(d => d.Id == analysis.DefectId)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.AIDescription, description)
                        .SetProperty(d => d.AIConfidence, confidence)
                        .SetProperty(d => d.LatestAiAnalysisId, analysis.Id)
                        .SetProperty(d => d.UpdatedAt, now), ct);
                await db.InspectionDefects
                    .Where(d => d.Id == analysis.DefectId && (d.FinalDescription == null || d.FinalDescription == ""))
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.FinalDescription, description), ct);
                break;
            case AiAnalysisKind.RoomComparison:
                await db.InspectionComparisons.Where(c => c.Id == analysis.ComparisonId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.AIAnalysis, description)
                        .SetProperty(c => c.LatestAiAnalysisId, analysis.Id)
                        .SetProperty(c => c.UpdatedAt, now), ct);
                break;
        }
    }

    private async Task<ComparisonAnalysisResult> CompareAsync(InspectionRoom room, IReadOnlyList<AnalysisImage> currentImages, AiOutputLanguage language, CancellationToken ct)
    {
        var comparison = await db.InspectionComparisons.AsNoTracking().FirstAsync(c => c.InspectionRoomId == room.Id, ct);
        var baseline = await snapshots.FindRoomAsync(comparison.SourceInspectionId, comparison.SourceInspectionRoomId, ct);
        var baselineImages = new List<AnalysisImage>();
        foreach (var photo in (baseline?.Photos ?? []).Take(AiAnalysisService.MaxImagesPerAnalysis / 2))
            baselineImages.Add(new AnalysisImage(photo.MimeType, await ReadAllAsync(photo.StorageKey, ct)));

        var currentDefects = await db.InspectionDefects.AsNoTracking().Where(d => d.InspectionRoomId == room.Id)
            .Select(d => d.FinalDescription ?? d.Description ?? "Defect").ToListAsync(ct);
        return await analyzer.CompareRoomAsync(new RoomComparisonRequest(
            room.Name,
            baseline?.Description,
            baseline?.Defects.Select(d => d.Description ?? d.Title ?? "Defect").ToList() ?? [],
            baselineImages,
            room.FinalDescription,
            currentDefects,
            currentImages.Take(AiAnalysisService.MaxImagesPerAnalysis / 2).ToList(),
            language), ct);
    }

    private async Task<IReadOnlyList<AnalysisImage>> LoadImagesAsync(IReadOnlyList<Guid> mediaIds, CancellationToken ct)
    {
        var media = await db.InspectionRoomMedia.AsNoTracking()
            .Where(m => mediaIds.Contains(m.Id) && m.Status == MediaStatus.Ready).ToListAsync(ct);
        var images = new List<AnalysisImage>(media.Count);
        foreach (var m in media) images.Add(new AnalysisImage(m.MimeType, await ReadAllAsync(m.StorageKey, ct)));
        if (images.Count == 0) throw new AiProviderException(AiErrorTexts.PhotosGone);
        return images;
    }

    private async Task<byte[]> ReadAllAsync(string key, CancellationToken ct)
    {
        await using var stream = await storage.OpenReadAsync(key, ct);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}

/// <summary>Provider failure with a message that is safe to show to users (never contains secrets).</summary>
/// <summary>An expected AI failure whose text is safe to show; stored in English (see <see cref="AiErrorTexts"/>).</summary>
public sealed class AiProviderException(LocalizedText text, Exception? inner = null) : Exception(text.En, inner);

using System.Text.Json;
using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.AI.Application;

public sealed record AiAnalysisDto(Guid Id, string Kind, string Status, string Provider, string? Model, bool IsMock,
    string? Description, decimal? Confidence, string? Error, DateTimeOffset RequestedAt, DateTimeOffset? CompletedAt,
    JsonElement? Result);

public sealed record AiStatusDto(string Provider, string? Model, bool IsMock, bool Available);

/// <summary>Queue for background AI processing (in-process channel in the MVP).</summary>
public interface IAiAnalysisQueue
{
    ValueTask EnqueueAsync(Guid analysisId, CancellationToken cancellationToken = default);
}

public sealed class AiAnalysisService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionWriteScope writeScope,
    IImageAnalysisService analyzer,
    IAiAnalysisQueue queue,
    IAuditLogger audit,
    IClock clock)
{
    public const int MaxImagesPerAnalysis = 8;

    public AiStatusDto GetStatus() => new(analyzer.ProviderName, analyzer.Model, analyzer.IsMock, true);

    public Task<AiAnalysisDto> RequestRoomAnalysisAsync(Guid inspectionId, Guid roomId, CancellationToken ct) =>
        RequestAsync(inspectionId, roomId, AiAnalysisKind.RoomDescription, null, ct);

    public Task<AiAnalysisDto> RequestDefectAnalysisAsync(Guid inspectionId, Guid roomId, Guid defectId, CancellationToken ct) =>
        RequestAsync(inspectionId, roomId, AiAnalysisKind.DefectDescription, defectId, ct);

    public Task<AiAnalysisDto> RequestComparisonAnalysisAsync(Guid inspectionId, Guid roomId, CancellationToken ct) =>
        RequestAsync(inspectionId, roomId, AiAnalysisKind.RoomComparison, null, ct);

    public async Task<AiAnalysisDto> GetAsync(Guid inspectionId, Guid analysisId, CancellationToken ct)
    {
        await access.GetForAssignedAgentAsync(inspectionId, ct, tracking: false);
        var analysis = await db.AiAnalyses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == analysisId && a.InspectionId == inspectionId, ct)
                       ?? throw new NotFoundException(EntityNames.Analysis, analysisId);
        return ToDto(analysis, analyzer.IsMock);
    }

    private async Task<Guid> CreateRequestAsync(Guid inspectionId, Guid roomId, AiAnalysisKind kind, Guid? defectId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        inspection.EnsureEditableBy(agentId);
        var room = inspection.Rooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException(EntityNames.Room, roomId);
        if (defectId is not null) room.GetDefect(defectId.Value);

        Guid? comparisonId = null;
        if (kind == AiAnalysisKind.RoomComparison)
        {
            comparisonId = await db.InspectionComparisons.Where(c => c.InspectionRoomId == roomId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct)
                           ?? throw new DomainRuleException("comparison.none", InspectionMessages.NoBaseline);
        }

        // Idempotency: one running job per target.
        var running = await db.AiAnalyses.AsNoTracking()
            .Where(a => a.InspectionRoomId == roomId && a.Kind == kind && a.DefectId == defectId &&
                        (a.Status == AiAnalysisStatus.Pending || a.Status == AiAnalysisStatus.Processing))
            .FirstOrDefaultAsync(ct);
        if (running is not null) return running.Id;

        var mediaType = kind == AiAnalysisKind.DefectDescription ? MediaType.Defect : MediaType.General;
        var mediaIds = await db.InspectionRoomMedia.AsNoTracking()
            .Where(m => m.InspectionRoomId == roomId && m.MediaType == mediaType && m.DefectId == defectId && m.Status == MediaStatus.Ready)
            .OrderByDescending(m => m.UploadedAt).Take(MaxImagesPerAnalysis).Select(m => m.Id).ToListAsync(ct);
        if (mediaIds.Count == 0)
            throw new DomainRuleException("ai.no_photos", kind == AiAnalysisKind.DefectDescription
                ? new("Upload at least one photo of the defect before requesting an AI description.",
                    "Envie pelo menos uma foto da avaria antes de pedir uma descrição por IA.")
                : new("Upload at least one general photo before requesting an AI description.",
                    "Envie pelo menos uma foto geral antes de pedir uma descrição por IA."));

        var now = clock.UtcNow;
        var analysis = new AiAnalysis
        {
            Id = Guid.NewGuid(),
            InspectionId = inspection.Id,
            InspectionRoomId = room.Id,
            DefectId = defectId,
            ComparisonId = comparisonId,
            Kind = kind,
            Status = AiAnalysisStatus.Pending,
            Provider = analyzer.ProviderName,
            Model = analyzer.Model,
            PromptVersion = AiPrompts.FullVersion(await AiLanguageOfAsync(inspection.CompanyId, ct)),
            MediaIds = mediaIds,
            RequestedBy = agentId,
            RequestedAt = now,
        };
        db.AiAnalyses.Add(analysis);
        room.MarkActivity(now);
        audit.Record(AuditActions.AIAnalysisRequested, nameof(AiAnalysis), analysis.Id,
            new { inspectionId, roomId, defectId, kind = kind.ToString(), provider = analyzer.ProviderName, images = mediaIds.Count });
        await db.SaveChangesAsync(ct);
        return analysis.Id;
    }

    private async Task<AiAnalysisDto> RequestAsync(Guid inspectionId, Guid roomId, AiAnalysisKind kind, Guid? defectId, CancellationToken ct)
    {
        var id = await writeScope.RunAsync(inspectionId, () => CreateRequestAsync(inspectionId, roomId, kind, defectId, ct), ct);
        var existing = await db.AiAnalyses.AsNoTracking().FirstAsync(a => a.Id == id, ct);
        // Enqueueing twice is harmless: the processor claims a job atomically.
        if (existing.Status == AiAnalysisStatus.Pending) await queue.EnqueueAsync(id, ct);

        var fresh = await db.AiAnalyses.AsNoTracking().FirstAsync(a => a.Id == id, ct);
        return ToDto(fresh, analyzer.IsMock);
    }

    public static AiAnalysisDto ToDto(AiAnalysis a, bool isMock) => new(a.Id, a.Kind.ToString(), a.Status.ToString(), a.Provider,
        a.Model, isMock && a.Provider == MockProviderName, a.Description, a.Confidence, AiErrorTexts.Localize(a.Error), a.RequestedAt, a.CompletedAt,
        a.ResultJson is null ? null : JsonDocument.Parse(a.ResultJson).RootElement.Clone());

    public const string MockProviderName = "mock";

    /// <summary>The company's report language decides the language of every AI text (see AiOutputLanguage).</summary>
    internal async Task<AiOutputLanguage> AiLanguageOfAsync(Guid companyId, CancellationToken ct) =>
        AiOutputLanguage.For(await db.Companies.Where(c => c.Id == companyId).Select(c => c.ReportLanguage).FirstOrDefaultAsync(ct));
}

using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Gathers facts owned by other modules (media, AI jobs, comparisons) needed for room validation.</summary>
public sealed class RoomContextLoader(IAppDbContext db, IOptions<InspectionRulesOptions> rules)
{
    public async Task<IReadOnlyDictionary<Guid, RoomCompletionContext>> LoadAsync(Inspection inspection, CancellationToken ct)
    {
        var roomIds = inspection.Rooms.Select(r => r.Id).ToList();
        var media = await db.InspectionRoomMedia.AsNoTracking()
            .Where(m => m.InspectionId == inspection.Id)
            .Select(m => new { m.InspectionRoomId, m.MediaType, m.DefectId, m.Status })
            .ToListAsync(ct);
        var runningAnalyses = await db.AiAnalyses.AsNoTracking()
            .Where(a => a.InspectionId == inspection.Id &&
                        (a.Status == AiAnalysisStatus.Pending || a.Status == AiAnalysisStatus.Processing))
            .GroupBy(a => a.InspectionRoomId).Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var comparisons = await db.InspectionComparisons.AsNoTracking()
            .Where(c => c.TargetInspectionId == inspection.Id)
            .Select(c => new { c.InspectionRoomId, Decided = c.AgentDecision != null })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, RoomCompletionContext>();
        foreach (var roomId in roomIds)
        {
            var roomMedia = media.Where(m => m.InspectionRoomId == roomId).ToList();
            var comparison = comparisons.FirstOrDefault(c => c.InspectionRoomId == roomId);
            result[roomId] = new RoomCompletionContext(
                GeneralPhotoCount: roomMedia.Count(m => m.MediaType == MediaType.General && m.Status == MediaStatus.Ready),
                DefectPhotoCounts: roomMedia.Where(m => m.MediaType == MediaType.Defect && m.DefectId != null && m.Status == MediaStatus.Ready)
                    .GroupBy(m => m.DefectId!.Value).ToDictionary(g => g.Key, g => g.Count()),
                PendingUploads: roomMedia.Count(m => m.Status == MediaStatus.Pending),
                PendingAnalyses: runningAnalyses.FirstOrDefault(a => a.RoomId == roomId)?.Count ?? 0,
                ComparisonRequired: comparison is not null,
                ComparisonDecided: comparison?.Decided ?? false,
                MinimumGeneralPhotos: rules.Value.MinimumGeneralPhotosPerRoom);
        }
        return result;
    }

    public async Task<RoomCompletionContext> LoadForRoomAsync(Inspection inspection, Guid roomId, CancellationToken ct) =>
        (await LoadAsync(inspection, ct))[roomId];
}

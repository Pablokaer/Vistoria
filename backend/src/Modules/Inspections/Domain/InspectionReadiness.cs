using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>Aggregates room-level issues into inspection-level readiness (for Review and Finalize).</summary>
public static class InspectionReadiness
{
    public static IReadOnlyList<LocalizedText> GetBlockingIssues(Inspection inspection,
        IReadOnlyDictionary<Guid, RoomCompletionContext> contexts, bool requireRoomsCompleted)
    {
        var issues = new List<LocalizedText>();
        foreach (var room in inspection.Rooms.Where(r => r.IsRequired).OrderBy(r => r.Sequence))
        {
            if (requireRoomsCompleted && room.Status != InspectionRoomStatus.Completed)
            {
                issues.Add(new($"{room.Name}: not marked as complete.", $"{room.Name}: não marcado como concluído."));
                continue;
            }
            var context = contexts.TryGetValue(room.Id, out var c) ? c : new RoomCompletionContext(0, new Dictionary<Guid, int>());
            issues.AddRange(room.GetCompletionIssues(context));
        }
        return issues;
    }
}

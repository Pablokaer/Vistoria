using InspectFlow.Modules.Reports.Application;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Figures for the inspection summary, derived only from the snapshot (nothing estimated).
/// Example: <c>ReportStatistics.From(snapshot).RoomsWithoutDefects</c>.
/// </summary>
public sealed record ReportStatistics(
    int RoomsInspected,
    int DefectsRecorded,
    int RoomsWithDefects,
    int RoomsWithoutDefects,
    int PhotosRecorded,
    IReadOnlyList<KeyValuePair<string, int>> DefectsByClassification,
    IReadOnlyList<KeyValuePair<string, int>> ComparisonDecisions)
{
    // Most serious first, so the summary reads in order of importance.
    private static readonly string[] ClassificationOrder = ["NewDamage", "PreExisting", "NormalWear", "Resolved", "Unchanged", "Unknown"];

    public static ReportStatistics From(ReportSnapshot snapshot)
    {
        var rooms = snapshot.Rooms;
        var defects = rooms.SelectMany(r => r.Defects).ToList();
        var withDefects = rooms.Count(r => r.Defects.Count > 0);
        var photos = rooms.Sum(r => r.Photos.Count + r.Defects.Sum(d => d.Photos.Count));
        return new ReportStatistics(rooms.Count, defects.Count, withDefects, rooms.Count - withDefects, photos,
            Ordered(defects.GroupBy(d => d.Classification).ToDictionary(g => g.Key, g => g.Count())),
            Ordered(snapshot.Comparison?.DecisionCounts ?? new Dictionary<string, int>()));
    }

    private static List<KeyValuePair<string, int>> Ordered(IReadOnlyDictionary<string, int> counts) =>
        counts.Where(kv => kv.Value > 0)
            .OrderBy(kv => Array.IndexOf(ClassificationOrder, kv.Key) is var i && i < 0 ? int.MaxValue : i)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
}

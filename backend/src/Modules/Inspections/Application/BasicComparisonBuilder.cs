using System.Text;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Reports.Application;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>
/// Deterministic, text-based comparison of a room between the baseline (Move In) report and the current record.
/// It only highlights possible differences; the agent decides.
/// </summary>
public static class BasicComparisonBuilder
{
    public sealed record Result(string Text, int PossibleDifferences);

    public static Result Build(
        ReportRoom baseline,
        RoomAnalysisResult? baselineAi,
        string? currentDescription,
        IReadOnlyList<InspectionDefect> currentDefects,
        RoomAnalysisResult? currentAi)
    {
        var sb = new StringBuilder();
        var differences = 0;

        sb.AppendLine($"Move In description: {Trim(baseline.Description) ?? "(none recorded)"}");
        sb.AppendLine($"Current description: {Trim(currentDescription) ?? "(not written yet)"}");

        if (baselineAi is not null && currentAi is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Surface conditions (from AI observations, advisory):");
            differences += Surface(sb, "Walls", baselineAi.Walls, currentAi.Walls);
            differences += Surface(sb, "Ceiling", baselineAi.Ceiling, currentAi.Ceiling);
            differences += Surface(sb, "Floor", baselineAi.Floor, currentAi.Floor);

            var before = baselineAi.VisibleItems.Select(i => i.Name.Trim().ToLowerInvariant()).ToHashSet();
            var after = currentAi.VisibleItems.Select(i => i.Name.Trim().ToLowerInvariant()).ToHashSet();
            var missing = before.Except(after).ToList();
            var added = after.Except(before).ToList();
            if (missing.Count > 0)
            {
                differences += missing.Count;
                sb.AppendLine($"- Items noted at Move In but not identified now: {string.Join(", ", missing)}");
            }
            if (added.Count > 0)
                sb.AppendLine($"- Items identified now but not noted at Move In: {string.Join(", ", added)}");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("Structured surface comparison unavailable (AI observations missing for one of the inspections).");
        }

        sb.AppendLine();
        sb.AppendLine($"Defects recorded at Move In: {baseline.Defects.Count}. Defects recorded now: {currentDefects.Count}.");
        foreach (var d in baseline.Defects)
            sb.AppendLine($"- Move In: {Trim(d.Title ?? d.Description, 160)}");
        foreach (var d in currentDefects)
        {
            var label = Trim(d.Description ?? d.FinalDescription, 160) ?? "Defect";
            sb.AppendLine($"- Now: {label} (classification: {d.Classification})");
            if (d.Classification is DefectClassification.NewDamage or DefectClassification.Unknown) differences++;
        }

        sb.AppendLine();
        sb.Append(differences == 0
            ? "No possible differences were identified from the recorded data. The inspector must still confirm."
            : $"{differences} possible difference(s) identified. The inspector must review and confirm each one.");
        return new Result(sb.ToString(), differences);
    }

    private static readonly Dictionary<string, int> Rank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["good"] = 0, ["fair"] = 1, ["poor"] = 2,
    };

    private static int Surface(StringBuilder sb, string name, SurfaceObservation? before, SurfaceObservation? after)
    {
        if (before is null || after is null)
        {
            sb.AppendLine($"- {name}: not comparable");
            return 0;
        }
        var worse = Rank.TryGetValue(before.Condition, out var b) && Rank.TryGetValue(after.Condition, out var a) && a > b;
        var colourChanged = before.Color is not null && after.Color is not null &&
                            !string.Equals(before.Color, after.Color, StringComparison.OrdinalIgnoreCase);
        var note = worse ? " (possible deterioration)" : colourChanged ? " (colour appears different)" : " (no change noted)";
        sb.AppendLine($"- {name}: {Describe(before)} → {Describe(after)}{note}");
        return worse || colourChanged ? 1 : 0;
    }

    private static string Describe(SurfaceObservation s) =>
        string.Join(" ", new[] { s.Color, s.Material, s.Condition }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string? Trim(string? value, int max = 400)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ReplaceLineEndings(" ");
        return v.Length <= max ? v : v[..max] + "…";
    }
}

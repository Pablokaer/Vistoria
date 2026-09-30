using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Reports.Application;

namespace InspectFlow.Tests.Domain;

public class BasicComparisonBuilderTests
{
    private static RoomAnalysisResult Ai(string floorCondition, params string[] items) => new("Bedroom",
        new SurfaceObservation("white", "paint", "good", null), new SurfaceObservation("white", "paint", "good", null),
        new SurfaceObservation("beige", "carpet", floorCondition, null), items.Select(i => new VisibleItem(i, "good", null)).ToList(),
        [], null, [], "desc");

    private static ReportRoom Baseline(params ReportDefect[] defects) => new(Guid.NewGuid(), Guid.NewGuid(), "Bedroom 1", "Bedroom", 1,
        "Carpet in good condition.", null, null, "Agent", null, defects.Length > 0, [], defects, null);

    [Fact]
    public void Flags_deterioration_missing_items_and_new_damage()
    {
        var newDamage = new InspectionDefect { Id = Guid.NewGuid(), Description = "Carpet stain", Classification = DefectClassification.NewDamage };
        var result = BasicComparisonBuilder.Build(Baseline(), Ai("good", "bed", "wardrobe"), "Stained carpet.", [newDamage], Ai("poor", "bed"));
        Assert.Contains("possible deterioration", result.Text, StringComparison.Ordinal);
        Assert.Contains("wardrobe", result.Text, StringComparison.Ordinal);
        Assert.Contains("Carpet stain", result.Text, StringComparison.Ordinal);
        Assert.Equal(3, result.PossibleDifferences);
        Assert.Contains("must review", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_no_differences_but_still_requires_the_inspector()
    {
        var result = BasicComparisonBuilder.Build(Baseline(), Ai("good", "bed"), "Same.", [], Ai("good", "bed"));
        Assert.Equal(0, result.PossibleDifferences);
        Assert.Contains("must still confirm", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Works_without_structured_ai_data()
    {
        var result = BasicComparisonBuilder.Build(Baseline(), null, null, [], null);
        Assert.Contains("unavailable", result.Text, StringComparison.Ordinal);
    }
}

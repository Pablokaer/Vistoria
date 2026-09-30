using System.Security.Cryptography;
using InspectFlow.Modules.AI.Application;

namespace InspectFlow.Infrastructure.AI;

/// <summary>
/// Development-only provider used when no OpenAI key is configured. Output is deterministic,
/// plausible and explicitly labelled as mock — images are NOT analysed.
/// </summary>
public sealed class MockImageAnalysisService : IImageAnalysisService
{
    public const string Label = "[Development mock AI — photos were not actually analysed]";

    public string ProviderName => AiAnalysisService.MockProviderName;
    public string? Model => "mock-v1";
    public bool IsMock => true;

    public async Task<RoomAnalysisResult> AnalyzeRoomAsync(RoomAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken); // Let the UI show its processing state.
        var seed = Seed(request.Images);
        var (floorMaterial, floorColour) = request.RoomType switch
        {
            "Kitchen" or "Bathroom" or "Utility" => ("ceramic tiles", "grey"),
            "Bedroom" => ("carpet", "beige"),
            "Garage" => ("concrete", "grey"),
            "Garden" or "Balcony" => ("paving", "grey"),
            _ => ("laminate", "light oak"),
        };
        var items = request.RoomType switch
        {
            "Kitchen" => new[] { "fitted wall and base units", "worktops", "sink with mixer tap", "oven and hob", "extractor hood" },
            "Bathroom" => ["bath with shower screen", "toilet", "wash basin", "mirror", "towel rail"],
            "Bedroom" => ["double bed frame", "wardrobe", "bedside table", "curtains"],
            "LivingRoom" => ["three-seat sofa", "coffee table", "TV unit", "curtains"],
            "Garage" => ["up-and-over door", "wall shelving", "light fitting"],
            _ => ["light fitting", "radiator"],
        };
        var floorCondition = seed % 4 == 0 ? "fair" : "good";
        var description =
            $"{Label} {request.Images.Count} photo(s) of the {request.RoomName} were provided. " +
            $"Walls appear to be painted white and in good visible condition. The ceiling appears white with no visible marks. " +
            $"The floor is {floorColour} {floorMaterial} and appears in {floorCondition} visible condition. " +
            $"Visible items include: {string.Join(", ", items)}. " +
            "No visible damage is apparent in the provided images. Areas not shown in the photos have not been assessed.";

        return new RoomAnalysisResult(
            request.RoomType,
            new SurfaceObservation("white", "painted plaster", "good", null),
            new SurfaceObservation("white", "painted plaster", "good", null),
            new SurfaceObservation(floorColour, floorMaterial, floorCondition, floorCondition == "fair" ? "Light wear visible in places." : null),
            items.Select(i => new VisibleItem(i, "good", null)).ToList(),
            [],
            "Appears clean in the provided images.",
            ["Development mock provider: images were not actually analysed."],
            description);
    }

    public async Task<DefectAnalysisResult> AnalyzeDefectAsync(DefectAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        var label = string.IsNullOrWhiteSpace(request.AgentHint) ? "a mark" : request.AgentHint.Trim();
        return new DefectAnalysisResult(
            $"Possible {label}",
            $"{Label} The photo(s) appear to show {label.ToLowerInvariant()} in the {request.RoomName}. " +
            "The extent is limited to the area visible in the images; the cause cannot be determined from the photos.",
            null,
            0.5m);
    }

    public async Task<ComparisonAnalysisResult> CompareRoomAsync(RoomComparisonRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        var differences = new List<PossibleDifference>();
        foreach (var defect in request.CurrentDefects.Where(d => !request.BaselineDefects.Contains(d)))
            differences.Add(new PossibleDifference("Recorded defect", "Not recorded at baseline", defect, "NewDamage"));
        foreach (var defect in request.BaselineDefects.Where(d => request.CurrentDefects.Contains(d)))
            differences.Add(new PossibleDifference("Recorded defect", defect, defect, "PreExisting"));

        var summary = differences.Count == 0
            ? $"{Label} No possible differences were identified between the baseline and current records of the {request.RoomName}."
            : $"{Label} {differences.Count} possible difference(s) identified in the {request.RoomName}. The inspector must confirm each one.";
        return new ComparisonAnalysisResult(differences, summary, 0.4m);
    }

    private static int Seed(IReadOnlyList<AnalysisImage> images)
    {
        if (images.Count == 0) return 1;
        var hash = SHA256.HashData(images[0].Content);
        return hash[0];
    }
}

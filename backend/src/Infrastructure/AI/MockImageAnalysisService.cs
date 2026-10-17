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

    /// <summary>
    /// The mock cannot translate; it names the language a real provider would use so the setting is visible in development.
    /// Example: <c>LabelFor(AiOutputLanguage.PortugueseBrazil) // "[Development mock AI — …; a real provider writes in Brazilian Portuguese]"</c>
    /// </summary>
    public static string LabelFor(AiOutputLanguage language) => $"{Label[..^1]}; a real provider writes in {language.PromptName}]";

    public string ProviderName => AiAnalysisService.MockProviderName;
    public string? Model => "mock-v1";
    public bool IsMock => true;

    public async Task<RoomAnalysisResult> AnalyzeRoomAsync(RoomAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken); // Let the UI show its processing state.
        var floor = MockFloor(request.RoomType, floorCondition: Seed(request.Images) % 4 == 0 ? "fair" : "good");
        var items = MockItems(request.RoomType);
        return new RoomAnalysisResult(
            request.RoomType,
            new SurfaceObservation("white", "painted plaster", "good", null),
            new SurfaceObservation("white", "painted plaster", "good", null),
            floor,
            items.Select(i => new VisibleItem(i, "good", null)).ToList(),
            [],
            "The room appears clean.",
            ["Development mock provider: images were not actually analysed."],
            $"{LabelFor(request.Language)} {MockRoomDescription(request.RoomName, floor, items)}");
    }

    /// <summary>
    /// The mock's room paragraph in the writing guide's report voice and order (room → ceiling → walls → floor →
    /// items → defects → cleanliness), so development drafts look like the real ones.
    /// Example: <c>MockRoomDescription("Bedroom 1", floor, ["wardrobe"]) // "Bedroom 1, furnished. White ceiling …"</c>
    /// </summary>
    public static string MockRoomDescription(string roomName, SurfaceObservation floor, IReadOnlyList<string> items)
    {
        var floorText = floor.Condition == "fair" ? "with light wear in places" : "in good visible condition";
        return $"{roomName}, furnished. White ceiling with a light fitting, in good visible condition. " +
               "Walls painted white with white skirting, in good visible condition. " +
               $"{Capitalize(floor.Color!)} {floor.Material} floor, {floorText}. " +
               $"{Capitalize(string.Join(", ", items))}, in good visible condition. " +
               "No visible damage noted. The room appears clean.";
    }

    private static SurfaceObservation MockFloor(string roomType, string floorCondition)
    {
        var (material, colour) = roomType switch
        {
            "Kitchen" or "Bathroom" or "Utility" => ("ceramic tile", "grey"),
            "Bedroom" => ("carpet", "beige"),
            "Garage" => ("concrete", "grey"),
            "Garden" or "Balcony" => ("paving", "grey"),
            _ => ("laminate", "light oak"),
        };
        return new SurfaceObservation(colour, material, floorCondition, floorCondition == "fair" ? "Light wear in places." : null);
    }

    private static string[] MockItems(string roomType) => roomType switch
    {
        "Kitchen" => ["fitted wall and base units", "worktops", "sink with mixer tap", "oven and hob", "extractor hood"],
        "Bathroom" => ["bath with shower screen", "toilet", "wash basin", "mirror", "towel rail"],
        "Bedroom" => ["double bed frame", "wardrobe", "bedside table", "curtains"],
        "LivingRoom" => ["three-seat sofa", "coffee table", "TV unit", "curtains"],
        "Garage" => ["up-and-over door", "wall shelving", "light fitting"],
        _ => ["light fitting", "radiator"],
    };

    public async Task<DefectAnalysisResult> AnalyzeDefectAsync(DefectAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        var label = string.IsNullOrWhiteSpace(request.AgentHint) ? "mark" : request.AgentHint.Trim();
        return new DefectAnalysisResult(
            Capitalize(label),
            $"{LabelFor(request.Language)} {MockDefectDescription(label, request.RoomName)}",
            null,
            0.5m);
    }

    /// <summary>
    /// The mock's defect sentence: starts from the inspector's label, as the guide asks, and does not invent a position
    /// or size it cannot know. Example: <c>MockDefectDescription("chipped tile", "Kitchen") // "Chipped tile noted in the Kitchen. …"</c>
    /// </summary>
    public static string MockDefectDescription(string inspectorLabel, string roomName) =>
        $"{Capitalize(inspectorLabel)} noted in the {roomName}. Exact size and position to be confirmed by the inspector.";

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    public async Task<ComparisonAnalysisResult> CompareRoomAsync(RoomComparisonRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        var differences = new List<PossibleDifference>();
        foreach (var defect in request.CurrentDefects.Where(d => !request.BaselineDefects.Contains(d)))
            differences.Add(new PossibleDifference("Recorded defect", "Not recorded at baseline", defect, "NewDamage"));
        foreach (var defect in request.BaselineDefects.Where(d => request.CurrentDefects.Contains(d)))
            differences.Add(new PossibleDifference("Recorded defect", defect, defect, "PreExisting"));

        var summary = differences.Count == 0
            ? $"{LabelFor(request.Language)} No possible differences were identified between the baseline and current records of the {request.RoomName}."
            : $"{LabelFor(request.Language)} {differences.Count} possible difference(s) identified in the {request.RoomName}. The inspector must confirm each one.";
        return new ComparisonAnalysisResult(differences, summary, 0.4m);
    }

    private static int Seed(IReadOnlyList<AnalysisImage> images)
    {
        if (images.Count == 0) return 1;
        var hash = SHA256.HashData(images[0].Content);
        return hash[0];
    }
}

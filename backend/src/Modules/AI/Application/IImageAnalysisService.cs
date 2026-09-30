
namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// Vendor-neutral image analysis. OpenAI is one implementation; a clearly-labelled mock is used in
/// development when no API key is configured. The domain never references any implementation.
/// </summary>
public interface IImageAnalysisService
{
    string ProviderName { get; }
    string? Model { get; }
    bool IsMock { get; }

    Task<RoomAnalysisResult> AnalyzeRoomAsync(RoomAnalysisRequest request, CancellationToken cancellationToken = default);

    Task<DefectAnalysisResult> AnalyzeDefectAsync(DefectAnalysisRequest request, CancellationToken cancellationToken = default);

    Task<ComparisonAnalysisResult> CompareRoomAsync(RoomComparisonRequest request, CancellationToken cancellationToken = default);
}

public sealed record AnalysisImage(string MimeType, byte[] Content);

/// <summary><c>Language</c> is the company's report language for every free-text value in the result.</summary>
public sealed record RoomAnalysisRequest(string RoomName, string RoomType, IReadOnlyList<AnalysisImage> Images, AiOutputLanguage Language);

public sealed record DefectAnalysisRequest(string RoomName, string? AgentHint, IReadOnlyList<AnalysisImage> Images, AiOutputLanguage Language);

public sealed record RoomComparisonRequest(
    string RoomName,
    string? BaselineDescription,
    IReadOnlyList<string> BaselineDefects,
    IReadOnlyList<AnalysisImage> BaselineImages,
    string? CurrentDescription,
    IReadOnlyList<string> CurrentDefects,
    IReadOnlyList<AnalysisImage> CurrentImages,
    AiOutputLanguage Language);

/// <summary>Condition vocabulary: "good", "fair", "poor", "not_visible".</summary>
public sealed record SurfaceObservation(string? Color, string? Material, string Condition, string? Notes);

public sealed record VisibleItem(string Name, string? Condition, string? Notes);

public sealed record ObservedDefect(string Location, string Description, string? Severity);

public sealed record RoomAnalysisResult(
    string RoomType,
    SurfaceObservation? Walls,
    SurfaceObservation? Ceiling,
    SurfaceObservation? Floor,
    IReadOnlyList<VisibleItem> VisibleItems,
    IReadOnlyList<ObservedDefect> ObservedDefects,
    string? Cleanliness,
    IReadOnlyList<string> Limitations,
    string Description);

public sealed record DefectAnalysisResult(string Summary, string Description, string? Location, decimal Confidence);

public sealed record PossibleDifference(string Area, string Baseline, string Current, string SuggestedClassification);

public sealed record ComparisonAnalysisResult(IReadOnlyList<PossibleDifference> PossibleDifferences, string Summary, decimal Confidence);

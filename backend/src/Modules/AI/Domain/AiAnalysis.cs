namespace InspectFlow.Modules.AI.Domain;

public enum AiAnalysisKind
{
    RoomDescription,
    DefectDescription,
    RoomComparison,
}

public enum AiAnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
}

/// <summary>
/// One AI request and its structured result. Kept forever for traceability:
/// which provider/model/prompt produced which text from which photos.
/// </summary>
public class AiAnalysis
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public Guid InspectionRoomId { get; set; }
    public Guid? DefectId { get; set; }
    public Guid? ComparisonId { get; set; }
    public AiAnalysisKind Kind { get; set; }
    public AiAnalysisStatus Status { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public List<Guid> MediaIds { get; set; } = new();
    public string? ResultJson { get; set; }
    public string? Description { get; set; }
    public decimal? Confidence { get; set; }
    public string? Error { get; set; }
    public int Attempts { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsRunning => Status is AiAnalysisStatus.Pending or AiAnalysisStatus.Processing;
}

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>
/// Per-room comparison between a baseline (source, usually Move In) and the current (target, usually Move Out)
/// inspection. The AI may suggest differences; only <see cref="AgentDecision"/> is authoritative.
/// </summary>
public class InspectionComparison
{
    public Guid Id { get; set; }
    public Guid SourceInspectionId { get; set; }
    public Guid TargetInspectionId { get; set; }

    /// <summary>The room of the target inspection.</summary>
    public Guid InspectionRoomId { get; set; }
    public Guid SourceInspectionRoomId { get; set; }

    /// <summary>Deterministic, text-based comparison built from the recorded data.</summary>
    public string? BasicComparison { get; set; }

    /// <summary>AI-assisted (visual) comparison, advisory only.</summary>
    public string? AIAnalysis { get; set; }
    public Guid? LatestAiAnalysisId { get; set; }

    public ComparisonDecision? AgentDecision { get; private set; }
    public string? AgentNotes { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void RecordDecision(ComparisonDecision decision, string? notes, Guid agentId, DateTimeOffset now)
    {
        AgentDecision = decision;
        AgentNotes = Inspection.Normalize(notes, InspectionRoom.MaxTextLength);
        DecidedAt = now;
        DecidedBy = agentId;
        UpdatedAt = now;
    }
}

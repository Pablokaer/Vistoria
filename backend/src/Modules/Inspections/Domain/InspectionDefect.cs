namespace InspectFlow.Modules.Inspections.Domain;

public class InspectionDefect
{
    public Guid Id { get; set; }
    public Guid InspectionRoomId { get; set; }

    /// <summary>Short label, e.g. "Scuff mark beside door".</summary>
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DefectClassification Classification { get; set; }

    /// <summary>AI output, kept for traceability. Never overwritten by agent edits.</summary>
    public string? AIDescription { get; set; }
    public decimal? AIConfidence { get; set; }
    public Guid? LatestAiAnalysisId { get; set; }

    public string? FinalDescription { get; private set; }
    public bool AgentConfirmed { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Update(string? description, string? location, string? finalDescription,
        DefectClassification classification, bool agentConfirmed, DateTimeOffset now)
    {
        Description = Inspection.Normalize(description, 300);
        Location = Inspection.Normalize(location, 200);
        FinalDescription = Inspection.Normalize(finalDescription, InspectionRoom.MaxTextLength);
        Classification = classification;
        AgentConfirmed = agentConfirmed && FinalDescription is not null;
        UpdatedAt = now;
    }

    public void ApplyAiDescription(Guid analysisId, string description, decimal? confidence, DateTimeOffset now)
    {
        AIDescription = description;
        AIConfidence = confidence;
        LatestAiAnalysisId = analysisId;
        if (string.IsNullOrWhiteSpace(FinalDescription)) FinalDescription = description;
        // AI output never counts as confirmation — the agent must confirm explicitly.
        UpdatedAt = now;
    }
}

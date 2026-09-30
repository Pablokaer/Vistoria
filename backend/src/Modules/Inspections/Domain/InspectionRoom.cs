using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>
/// Immutable snapshot of a PropertyRoom taken at publish time, plus the agent's findings for that room.
/// </summary>
public class InspectionRoom
{
    public const int MaxTextLength = 8000;

    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public Guid OriginalPropertyRoomId { get; set; }
    public RoomType RoomType { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public InspectionRoomStatus Status { get; set; }
    public bool IsRequired { get; set; } = true;

    /// <summary>For Move Out / Periodic: the matching room of the comparison (baseline) inspection.</summary>
    public Guid? ComparisonInspectionRoomId { get; set; }

    /// <summary>Latest AI-generated description. Never overwritten by agent edits.</summary>
    public string? AiDescription { get; set; }
    public Guid? LatestAiAnalysisId { get; set; }
    public DateTimeOffset? AiDescriptionGeneratedAt { get; set; }

    /// <summary>Description the agent stands behind (pre-filled from AI, then edited).</summary>
    public string? FinalDescription { get; private set; }
    public DescriptionSource FinalDescriptionSource { get; private set; }
    public DateTimeOffset? FinalDescriptionEditedAt { get; private set; }
    public Guid? FinalDescriptionEditedBy { get; private set; }

    public bool DefectsFound { get; private set; }
    public string? AgentNotes { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<InspectionDefect> Defects { get; } = new();

    public void MarkActivity(DateTimeOffset now)
    {
        if (Status == InspectionRoomStatus.Pending) Status = InspectionRoomStatus.InProgress;
        UpdatedAt = now;
    }

    /// <returns>True when the description actually changed (for auditing).</returns>
    public bool EditFinalDescription(string? text, Guid agentId, DateTimeOffset now)
    {
        var normalized = Inspection.Normalize(text, MaxTextLength);
        if (normalized == FinalDescription) return false;
        FinalDescription = normalized;
        FinalDescriptionSource = normalized is null ? DescriptionSource.None : DescriptionSource.Agent;
        FinalDescriptionEditedAt = now;
        FinalDescriptionEditedBy = agentId;
        MarkActivity(now);
        return true;
    }

    public void SetDefectsFound(bool value, DateTimeOffset now)
    {
        if (!value && Defects.Count > 0)
            throw new DomainRuleException("room.defects_present", new("Remove the recorded defects before unchecking 'Defects found'.", "Remova as avarias registradas antes de desmarcar 'Avarias encontradas'."));
        DefectsFound = value;
        MarkActivity(now);
    }

    public void SetAgentNotes(string? notes, DateTimeOffset now)
    {
        AgentNotes = Inspection.Normalize(notes, MaxTextLength);
        MarkActivity(now);
    }

    /// <summary>Stores a new AI result. Pre-fills the final description only if the agent has not written one.</summary>
    public void ApplyAiDescription(Guid analysisId, string description, DateTimeOffset now)
    {
        AiDescription = description;
        LatestAiAnalysisId = analysisId;
        AiDescriptionGeneratedAt = now;
        if (string.IsNullOrWhiteSpace(FinalDescription))
        {
            FinalDescription = description;
            FinalDescriptionSource = DescriptionSource.Ai;
        }
        MarkActivity(now);
    }

    public InspectionDefect AddDefect(string? description, string? location, Guid agentId, DateTimeOffset now)
    {
        var defect = new InspectionDefect
        {
            Id = Guid.NewGuid(),
            InspectionRoomId = Id,
            Description = Inspection.Normalize(description, 300),
            Location = Inspection.Normalize(location, 200),
            Classification = DefectClassification.Unknown,
            CreatedAt = now,
            CreatedBy = agentId,
            UpdatedAt = now,
        };
        Defects.Add(defect);
        DefectsFound = true;
        MarkActivity(now);
        return defect;
    }

    public InspectionDefect GetDefect(Guid defectId) =>
        Defects.FirstOrDefault(d => d.Id == defectId) ?? throw new NotFoundException(EntityNames.Defect, defectId);

    public void RemoveDefect(Guid defectId, DateTimeOffset now)
    {
        Defects.Remove(GetDefect(defectId));
        MarkActivity(now);
    }

    public IReadOnlyList<LocalizedText> GetCompletionIssues(RoomCompletionContext context)
    {
        var issues = new List<LocalizedText>();
        if (context.GeneralPhotoCount < context.MinimumGeneralPhotos)
            issues.Add(new($"{Name}: at least {context.MinimumGeneralPhotos} general photo(s) required.",
                $"{Name}: são necessárias pelo menos {context.MinimumGeneralPhotos} foto(s) gerais."));
        if (string.IsNullOrWhiteSpace(FinalDescription))
            issues.Add(new($"{Name}: a room description is required.", $"{Name}: a descrição do cômodo é obrigatória."));
        if (DefectsFound && Defects.Count == 0)
            issues.Add(new($"{Name}: 'Defects found' is checked but no defect has been recorded.",
                $"{Name}: 'Avarias encontradas' está marcado, mas nenhuma avaria foi registrada."));
        issues.AddRange(GetDefectIssues(context));
        issues.AddRange(GetPendingWorkIssues(context));
        return issues;
    }

    private IEnumerable<LocalizedText> GetDefectIssues(RoomCompletionContext context)
    {
        var index = 1;
        foreach (var defect in Defects.OrderBy(d => d.CreatedAt))
        {
            var label = new LocalizedText($"{Name}: defect #{index}", $"{Name}: avaria nº {index}");
            index++;
            if (string.IsNullOrWhiteSpace(defect.FinalDescription))
                yield return new($"{label.En} needs a description.", $"{label.PtBr} precisa de uma descrição.");
            if (context.DefectPhotoCounts.GetValueOrDefault(defect.Id) < 1)
                yield return new($"{label.En} needs at least one photo.", $"{label.PtBr} precisa de pelo menos uma foto.");
            if (!defect.AgentConfirmed)
                yield return new($"{label.En} must be confirmed by the agent.", $"{label.PtBr} precisa ser confirmada pelo vistoriador.");
        }
    }

    private IEnumerable<LocalizedText> GetPendingWorkIssues(RoomCompletionContext context)
    {
        if (context.PendingUploads > 0)
            yield return new($"{Name}: {context.PendingUploads} upload(s) still pending.", $"{Name}: {context.PendingUploads} envio(s) ainda pendente(s).");
        if (context.PendingAnalyses > 0)
            yield return new($"{Name}: AI analysis is still running.", $"{Name}: a análise por IA ainda está em andamento.");
        if (context.ComparisonRequired && !context.ComparisonDecided)
            yield return new($"{Name}: record your decision comparing this room with the Move In inspection.",
                $"{Name}: registre sua decisão comparando este cômodo com a vistoria de entrada.");
    }

    public void Complete(RoomCompletionContext context, DateTimeOffset now)
    {
        var issues = GetCompletionIssues(context);
        if (issues.Count > 0)
            throw new DomainRuleException("room.incomplete", new($"{Name} cannot be completed yet.", $"{Name} ainda não pode ser concluído."), issues);
        Status = InspectionRoomStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Reopen(DateTimeOffset now)
    {
        if (Status != InspectionRoomStatus.Completed) return;
        Status = InspectionRoomStatus.InProgress;
        CompletedAt = null;
        UpdatedAt = now;
    }
}

/// <summary>Facts about a room that live outside the Inspections aggregate (media, AI jobs, comparisons).</summary>
public sealed record RoomCompletionContext(
    int GeneralPhotoCount,
    IReadOnlyDictionary<Guid, int> DefectPhotoCounts,
    int PendingUploads = 0,
    int PendingAnalyses = 0,
    bool ComparisonRequired = false,
    bool ComparisonDecided = false,
    int MinimumGeneralPhotos = 1);

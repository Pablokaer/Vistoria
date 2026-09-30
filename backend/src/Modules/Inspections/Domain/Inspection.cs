using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>Data copied from a PropertyRoom when an inspection is published.</summary>
public sealed record RoomSnapshotSource(Guid PropertyRoomId, RoomType RoomType, string Name, int Sequence,
    Guid? ComparisonInspectionRoomId = null);

public class Inspection
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? AgentId { get; private set; }
    public InspectionType InspectionType { get; private set; }
    public InspectionVisibility Visibility { get; private set; }
    public InspectionStatus Status { get; private set; }
    public Guid? ComparisonInspectionId { get; private set; }
    public string? Instructions { get; private set; }
    public DateOnly? ScheduledDate { get; private set; }

    /// <summary>If still Open at this moment, the inspection expires.</summary>
    public DateTimeOffset? AcceptBy { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReviewStartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset? SentToTenantAt { get; private set; }
    public DateTimeOffset? TenantRespondedAt { get; private set; }
    public Guid? TenantRespondedBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }

    /// <summary>Optimistic concurrency token (mapped to PostgreSQL xmin).</summary>
    public uint Version { get; set; }

    public List<InspectionRoom> Rooms { get; } = new();

    public bool IsFinalized => InspectionStateMachine.IsFinalized(Status);

    public static Inspection CreateDraft(Guid companyId, Guid propertyId, Guid? tenancyId, InspectionType type,
        InspectionVisibility visibility, Guid? comparisonInspectionId, string? instructions, DateOnly? scheduledDate,
        DateTimeOffset? acceptBy, Guid createdBy, DateTimeOffset now)
    {
        if (type is InspectionType.MoveIn or InspectionType.MoveOut && tenancyId is null)
            throw new ValidationException("TenancyId", new($"A {type} inspection must belong to a tenancy.", $"Uma vistoria {InspectionTerms.Of(type).PtBr} precisa estar vinculada a uma locação."));
        if (comparisonInspectionId is not null && type is not (InspectionType.MoveOut or InspectionType.Periodic))
            throw new ValidationException("ComparisonInspectionId",
                new("Only Move Out and Periodic inspections can be compared with a previous inspection.", "Apenas vistorias de saída e periódicas podem ser comparadas com uma vistoria anterior."));
        if (acceptBy is not null && acceptBy <= now)
            throw new ValidationException("AcceptBy", new("The acceptance deadline must be in the future.", "O prazo para aceite precisa ser uma data futura."));

        return new Inspection
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PropertyId = propertyId,
            TenancyId = tenancyId,
            InspectionType = type,
            Visibility = visibility,
            Status = InspectionStatus.Draft,
            ComparisonInspectionId = comparisonInspectionId,
            Instructions = Normalize(instructions, 2000),
            ScheduledDate = scheduledDate,
            AcceptBy = acceptBy,
            CreatedAt = now,
            CreatedBy = createdBy,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Publishes the inspection and snapshots the property's rooms into <see cref="InspectionRoom"/>s.
    /// After this point, changes to PropertyRoom never affect this inspection.
    /// </summary>
    public void Publish(IReadOnlyList<RoomSnapshotSource> rooms, DateTimeOffset now)
    {
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Open);
        if (rooms.Count == 0)
            throw new DomainRuleException("inspection.no_rooms", new("Add at least one room to the property before publishing.", "Adicione pelo menos um cômodo ao imóvel antes de publicar."));
        if (AcceptBy is not null && AcceptBy <= now)
            throw new DomainRuleException("inspection.deadline_passed", new("The acceptance deadline has already passed.", "O prazo para aceite já passou."));

        var sequence = 1;
        foreach (var source in rooms.OrderBy(r => r.Sequence))
        {
            Rooms.Add(new InspectionRoom
            {
                Id = Guid.NewGuid(),
                InspectionId = Id,
                OriginalPropertyRoomId = source.PropertyRoomId,
                RoomType = source.RoomType,
                Name = source.Name,
                Sequence = sequence++,
                Status = InspectionRoomStatus.Pending,
                IsRequired = true,
                ComparisonInspectionRoomId = source.ComparisonInspectionRoomId,
                UpdatedAt = now,
            });
        }

        Status = InspectionStatus.Open;
        PublishedAt = now;
        UpdatedAt = now;
    }

    public void Accept(Guid agentId, DateTimeOffset now)
    {
        if (Status == InspectionStatus.Open && AcceptBy is not null && AcceptBy <= now)
            throw new DomainRuleException("inspection.expired", new("This inspection is no longer available.", "Esta vistoria não está mais disponível."));
        if (AgentId is not null || Status != InspectionStatus.Open)
            throw new DomainRuleException("inspection.not_available", InspectionMessages.NotAvailable);
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Assigned);
        AgentId = agentId;
        AcceptedAt = now;
        Status = InspectionStatus.Assigned;
        UpdatedAt = now;
    }

    public void Start(Guid agentId, DateTimeOffset now)
    {
        EnsureAssignedTo(agentId);
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.InProgress);
        Status = InspectionStatus.InProgress;
        StartedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Moves to Review. <paramref name="blockingIssues"/> is computed by <see cref="InspectionReadiness"/>.</summary>
    public void SubmitForReview(Guid agentId, IReadOnlyList<LocalizedText> blockingIssues, DateTimeOffset now)
    {
        EnsureAssignedTo(agentId);
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Review);
        if (blockingIssues.Count > 0)
            throw new DomainRuleException("inspection.not_ready", new("The inspection is not ready for review.", "A vistoria ainda não está pronta para revisão."), blockingIssues);
        Status = InspectionStatus.Review;
        ReviewStartedAt = now;
        UpdatedAt = now;
    }

    public void ReturnToInProgress(Guid agentId, DateTimeOffset now)
    {
        EnsureAssignedTo(agentId);
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.InProgress);
        Status = InspectionStatus.InProgress;
        UpdatedAt = now;
    }

    public void Finalize(Guid agentId, IReadOnlyList<LocalizedText> blockingIssues, DateTimeOffset now)
    {
        EnsureAssignedTo(agentId);
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Completed);
        if (blockingIssues.Count > 0)
            throw new DomainRuleException("inspection.not_ready", new("The inspection cannot be finalized yet.", "A vistoria ainda não pode ser finalizada."), blockingIssues);
        Status = InspectionStatus.Completed;
        CompletedAt = now;
        CompletedBy = agentId;
        UpdatedAt = now;
    }

    public void SendToTenant(DateTimeOffset now)
    {
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.AwaitingTenant);
        Status = InspectionStatus.AwaitingTenant;
        SentToTenantAt = now;
        UpdatedAt = now;
    }

    public void RecordTenantDecision(bool accepted, Guid tenantUserId, DateTimeOffset now)
    {
        var target = accepted ? InspectionStatus.Accepted : InspectionStatus.Disputed;
        InspectionStateMachine.EnsureCanTransition(Status, target);
        Status = target;
        TenantRespondedAt = now;
        TenantRespondedBy = tenantUserId;
        UpdatedAt = now;
    }

    public void Cancel(Guid userId, DateTimeOffset now)
    {
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Cancelled);
        Status = InspectionStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = userId;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        InspectionStateMachine.EnsureCanTransition(Status, InspectionStatus.Expired);
        if (AcceptBy is null || AcceptBy > now)
            throw new DomainRuleException("inspection.not_expired", new("The acceptance deadline has not passed.", "O prazo para aceite ainda não passou."));
        Status = InspectionStatus.Expired;
        ExpiredAt = now;
        UpdatedAt = now;
    }

    public void UpdateDraft(InspectionVisibility visibility, string? instructions, DateOnly? scheduledDate,
        DateTimeOffset? acceptBy, DateTimeOffset now)
    {
        if (Status != InspectionStatus.Draft)
            throw new DomainRuleException("inspection.not_draft", new("Only draft inspections can be edited.", "Apenas vistorias em rascunho podem ser editadas."));
        Visibility = visibility;
        Instructions = Normalize(instructions, 2000);
        ScheduledDate = scheduledDate;
        AcceptBy = acceptBy;
        UpdatedAt = now;
    }

    public bool IsAssignedTo(Guid agentId) => AgentId == agentId;

    public void EnsureAssignedTo(Guid agentId)
    {
        if (AgentId != agentId)
            throw new ForbiddenException(new("This inspection is not assigned to you.", "Esta vistoria não está atribuída a você."));
    }

    /// <summary>Guards every content change (rooms, photos, descriptions, defects, comparisons).</summary>
    public void EnsureEditableBy(Guid agentId)
    {
        EnsureAssignedTo(agentId);
        if (IsFinalized)
            throw new DomainRuleException("inspection.finalized", new("This inspection has been finalized and can no longer be changed.", "Esta vistoria foi finalizada e não pode mais ser alterada."));
        if (!InspectionStateMachine.IsEditable(Status))
            throw new DomainRuleException("inspection.not_editable",
                Status == InspectionStatus.Assigned
                    ? new("Start the inspection before recording information.", "Inicie a vistoria antes de registrar informações.")
                    : new($"The inspection cannot be edited while {Status}.", $"A vistoria não pode ser editada enquanto estiver {InspectionTerms.Of(Status).PtBr}."));
    }

    public void Touch(DateTimeOffset now) => UpdatedAt = now;

    internal static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ValidationException("Text", new($"Text must be at most {maxLength} characters.", $"O texto deve ter no máximo {maxLength} caracteres."));
        return trimmed;
    }
}

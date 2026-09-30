using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Tenancies.Domain;

public enum TenancyStatus
{
    Upcoming,
    Active,
    Ended,
    Cancelled,
}

/// <summary>A letting period of a property. A property has many tenancies over time; inspections hang off a tenancy.</summary>
public class Tenancy
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CompanyId { get; set; }
    public string? Reference { get; set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public TenancyStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<TenancyMember> Members { get; } = new();

    public static Tenancy Create(Guid companyId, Guid propertyId, DateOnly startDate, DateOnly? endDate,
        string? reference, DateTimeOffset now)
    {
        var tenancy = new Tenancy
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PropertyId = propertyId,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            CreatedAt = now,
        };
        tenancy.SetDates(startDate, endDate, now);
        tenancy.Status = startDate > DateOnly.FromDateTime(now.UtcDateTime) ? TenancyStatus.Upcoming : TenancyStatus.Active;
        return tenancy;
    }

    public void SetDates(DateOnly startDate, DateOnly? endDate, DateTimeOffset now)
    {
        if (endDate is not null && endDate < startDate)
            throw new ValidationException("EndDate", new("End date must be on or after the start date.", "A data de término deve ser igual ou posterior à data de início."));
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = now;
    }

    public void ChangeStatus(TenancyStatus status, DateTimeOffset now)
    {
        if (Status is TenancyStatus.Cancelled or TenancyStatus.Ended && status != Status)
            throw new DomainRuleException("tenancy.closed", new("An ended or cancelled tenancy cannot be reopened.", "Uma locação encerrada ou cancelada não pode ser reaberta."));
        Status = status;
        UpdatedAt = now;
    }
}

/// <summary>
/// A tenant of a tenancy. Created by the company with an email and a single-use invitation;
/// <see cref="UserId"/> is set only when the tenant accepts the invitation while signed in.
/// </summary>
public class TenancyMember
{
    public Guid Id { get; set; }
    public Guid TenancyId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? InvitationTokenHash { get; set; }
    public DateTimeOffset? InvitationExpiresAt { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
    public uint Version { get; set; }
}

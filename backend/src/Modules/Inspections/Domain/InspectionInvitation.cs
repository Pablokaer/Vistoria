using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>
/// Access to a private inspection: a high-entropy link token (stored as SHA-256) plus a 6-digit
/// access code (stored as a salted slow hash). Limited attempts, expiry and single use.
/// </summary>
public class InspectionInvitation
{
    public const int DefaultMaxAttempts = 5;

    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string AccessCodeHash { get; set; } = string.Empty;
    public string? InvitedEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; set; } = DefaultMaxAttempts;
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public Guid? UsedByUserId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public uint Version { get; set; }

    public bool IsLocked => AttemptCount >= MaxAttempts;

    /// <summary>Throws when the invitation cannot be used by <paramref name="agentId"/> any more.</summary>
    public void EnsureUsable(Guid agentId, DateTimeOffset now)
    {
        if (RevokedAt is not null) throw new DomainRuleException("invitation.revoked", new("This invitation is no longer valid.", "Este convite não é mais válido."));
        if (ExpiresAt <= now) throw new DomainRuleException("invitation.expired", new("This invitation has expired.", "Este convite expirou."));
        if (UsedByUserId is not null && UsedByUserId != agentId)
            throw new DomainRuleException("invitation.used", new("This invitation has already been used.", "Este convite já foi utilizado."));
        if (IsLocked && UsedByUserId is null)
            throw new TooManyAttemptsException(InspectionMessages.TooManyIncorrectCodes);
    }

    public void RegisterFailedAttempt(DateTimeOffset now)
    {
        AttemptCount++;
        LastAttemptAt = now;
    }

    public void MarkUsed(Guid agentId, DateTimeOffset now)
    {
        LastAttemptAt = now;
        UsedAt ??= now;
        UsedByUserId ??= agentId;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}

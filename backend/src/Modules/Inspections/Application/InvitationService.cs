using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>
/// Private inspection access: link token + 6-digit code. Failed attempts are counted per invitation
/// (in addition to HTTP rate limiting) and the invitation locks after MaxAttempts.
/// </summary>
public sealed class InvitationService(
    IAppDbContext db,
    InspectionAccess access,
    AgentInspectionService agentInspections,
    IAccessCodeHasher hasher,
    IAuditLogger audit,
    IClock clock)
{
    public async Task<InvitationPreviewDto> PreviewAsync(string token, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var invitation = await FindAsync(token, tracking: false, ct);
        invitation.EnsureUsable(agentId, clock.UtcNow);
        var verified = invitation.UsedByUserId == agentId;
        return new InvitationPreviewDto(!verified, invitation.ExpiresAt, Math.Max(0, invitation.MaxAttempts - invitation.AttemptCount),
            verified ? await TryGetPreviewAsync(invitation.InspectionId, ct) : null);
    }

    public async Task<InvitationPreviewDto> VerifyAsync(string token, string accessCode, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var now = clock.UtcNow;
        var invitation = await FindAsync(token, tracking: true, ct);
        invitation.EnsureUsable(agentId, now);

        if (invitation.UsedByUserId != agentId)
        {
            var code = (accessCode ?? string.Empty).Trim();
            var valid = code.Length == 6 && code.All(char.IsAsciiDigit) && hasher.Verify(invitation.AccessCodeHash, code);
            if (!valid)
            {
                invitation.RegisterFailedAttempt(now);
                audit.Record(AuditActions.InspectionInvitationFailedAttempt, nameof(Inspection), invitation.InspectionId,
                    new { invitationId = invitation.Id, attempt = invitation.AttemptCount });
                await SaveOrConflictAsync(ct);
                var remaining = invitation.MaxAttempts - invitation.AttemptCount;
                if (remaining <= 0)
                    throw new TooManyAttemptsException(InspectionMessages.TooManyIncorrectCodes);
                throw new ValidationException("AccessCode", new($"Incorrect access code. {remaining} attempt(s) remaining.", $"Código de acesso incorreto. {remaining} tentativa(s) restante(s)."));
            }

            invitation.MarkUsed(agentId, now);
            audit.Record(AuditActions.InspectionInvitationVerified, nameof(Inspection), invitation.InspectionId,
                new { invitationId = invitation.Id });
            await SaveOrConflictAsync(ct);
        }

        return new InvitationPreviewDto(false, invitation.ExpiresAt, Math.Max(0, invitation.MaxAttempts - invitation.AttemptCount),
            await TryGetPreviewAsync(invitation.InspectionId, ct));
    }

    private async Task<AvailableInspectionDto?> TryGetPreviewAsync(Guid inspectionId, CancellationToken ct)
    {
        try
        {
            return await agentInspections.GetAvailableAsync(inspectionId, ct);
        }
        catch (NotFoundException)
        {
            return null; // Already accepted (possibly by this agent) or no longer open.
        }
    }

    private async Task<InspectionInvitation> FindAsync(string token, bool tracking, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) throw new NotFoundException(EntityNames.Invitation);
        var hash = SecureTokens.Sha256Hex(token);
        var query = tracking ? db.InspectionInvitations : db.InspectionInvitations.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.TokenHash == hash, ct) ?? throw new NotFoundException(EntityNames.Invitation);
    }

    private async Task SaveOrConflictAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(new("Please try again.", "Tente novamente."));
        }
    }
}

/// <summary>Background maintenance: expires Open inspections whose acceptance deadline passed.</summary>
public sealed class InspectionMaintenanceService(IAppDbContext db, IAuditLogger audit, IClock clock)
{
    public async Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var overdue = await db.Inspections.Where(i => i.Status == InspectionStatus.Open && i.AcceptBy != null && i.AcceptBy <= now)
            .Take(100).ToListAsync(ct);
        foreach (var inspection in overdue)
        {
            inspection.Expire(now);
            audit.Record(AuditActions.InspectionExpired, nameof(Inspection), inspection.Id);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return 0; // Someone accepted meanwhile; next run will retry the rest.
        }
        return overdue.Count;
    }
}

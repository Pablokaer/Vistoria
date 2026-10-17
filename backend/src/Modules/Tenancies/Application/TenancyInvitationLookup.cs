using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Tenancies.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Tenancies.Application;

/// <summary>
/// Open tenant invitations (token stored as SHA-256, single use, expiring): found by token, and joined by a tenant
/// account. Shared by the tenant sign-up, which is only possible from an invitation (ADR 0023), and by signed-in
/// tenants accepting a further invitation. The caller saves the changes.
/// </summary>
public sealed class TenancyInvitationLookup(IAppDbContext db, IAuditLogger audit, IClock clock)
{
    private static readonly LocalizedText DifferentEmail = new(
        "This invitation was sent to a different email address.", "Este convite foi enviado para outro endereço de e-mail.");

    /// <summary>
    /// The not yet used, not expired invitation for <paramref name="token"/>, or 404.
    /// Example: <c>var member = await lookup.FindOpenAsync(token, ct, tracking: true);</c>
    /// </summary>
    public async Task<TenancyMember> FindOpenAsync(string? token, CancellationToken ct, bool tracking = false)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new NotFoundException(EntityNames.Invitation);
        var hash = SecureTokens.Sha256Hex(token);
        var query = tracking ? db.TenancyMembers : db.TenancyMembers.AsNoTracking();
        var member = await query.FirstOrDefaultAsync(m => m.InvitationTokenHash == hash, ct);
        if (member is null || member.UserId is not null || member.InvitationExpiresAt <= clock.UtcNow)
            throw new NotFoundException(EntityNames.Invitation);
        return member;
    }

    /// <summary>
    /// The invitation a new tenant account is created from: a token is required and the account's email must be the
    /// invited one, so nobody can claim a tenant address without the link sent to it.
    /// Example: <c>var member = await lookup.RequireForSignUpAsync(request.InvitationToken, request.Email, ct);</c>
    /// </summary>
    public async Task<TenancyMember> RequireForSignUpAsync(string? token, string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("InvitationToken", new(
                "Tenant accounts are created from the invitation link sent by the property manager (no invitation token given).",
                "Contas de inquilino são criadas a partir do link de convite enviado pela imobiliária (nenhum token de convite informado)."));
        var member = await FindOpenAsync(token, ct, tracking: true);
        EnsureInvitedEmail(member, email);
        return member;
    }

    /// <summary>Throws 403 unless <paramref name="email"/> is the invited address. Example: <c>lookup.EnsureInvitedEmail(member, user.Email)</c>.</summary>
    public void EnsureInvitedEmail(TenancyMember member, string? email)
    {
        if (!string.Equals(email?.Trim(), member.Email, StringComparison.OrdinalIgnoreCase)) throw new ForbiddenException(DifferentEmail);
    }

    /// <summary>Links the tenant account to the tenancy and spends the token. Example: <c>lookup.Join(member, userId)</c>.</summary>
    public void Join(TenancyMember member, Guid userId)
    {
        member.UserId = userId;
        member.JoinedAt = clock.UtcNow;
        member.InvitationTokenHash = null;
        audit.Record(AuditActions.TenantJoinedTenancy, nameof(Tenancy), member.TenancyId, new { memberId = member.Id }, userId);
    }
}

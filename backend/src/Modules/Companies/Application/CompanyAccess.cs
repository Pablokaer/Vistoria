using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Companies.Application;

/// <summary>
/// Resolves the caller's company from their membership (server side). Company ids sent by the client
/// are never trusted as proof of access.
/// </summary>
public sealed class CompanyAccess(IAppDbContext db, ICurrentUser currentUser)
{
    private CompanyMember? _membership;

    public async Task<CompanyMember> GetMembershipAsync(CancellationToken ct)
    {
        if (_membership is not null) return _membership;
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsInRole(AppRoles.Company))
            throw new ForbiddenException(new("Only company accounts can perform this action.", "Apenas contas de empresa podem realizar esta ação."));
        _membership = await db.CompanyMembers.AsNoTracking()
                          .Where(m => m.UserId == userId).OrderBy(m => m.CreatedAt).FirstOrDefaultAsync(ct)
                      ?? throw new DomainRuleException("company.workspace_required", new("Create your company workspace first.", "Crie primeiro o espaço da sua empresa."));
        return _membership;
    }

    /// <returns>The caller's company id, if they hold <paramref name="permission"/>.</returns>
    public async Task<Guid> RequireAsync(CompanyPermission permission, CancellationToken ct)
    {
        var membership = await GetMembershipAsync(ct);
        if (!CompanyRolePermissions.Has(membership.Role, permission))
            throw new ForbiddenException(new("Your company role does not allow this action.", "Seu papel na empresa não permite esta ação."));
        return membership.CompanyId;
    }

    /// <summary>True when the caller is a member of <paramref name="companyId"/> (no exception).</summary>
    public async Task<bool> IsMemberOfAsync(Guid companyId, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(AppRoles.Company)) return false;
        return await db.CompanyMembers.AsNoTracking().AnyAsync(m => m.UserId == userId && m.CompanyId == companyId, ct);
    }
}

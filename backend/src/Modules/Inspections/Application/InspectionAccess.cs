using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Inspections.Application;

public enum InspectionViewerKind
{
    Company,
    Agent,
    Tenant,
}

/// <summary>
/// Central authorization for inspections. Every lookup is scoped by the caller's identity so that
/// guessing an id yields "not found" instead of someone else's data.
/// </summary>
public sealed class InspectionAccess(IAppDbContext db, ICurrentUser currentUser, CompanyAccess companyAccess)
{
    /// <summary>Loads an inspection of the caller's company (tracked, with rooms and defects).</summary>
    public async Task<Inspection> GetForCompanyAsync(Guid inspectionId, CompanyPermission permission, CancellationToken ct, bool tracking = true)
    {
        var companyId = await companyAccess.RequireAsync(permission, ct);
        var query = tracking ? db.Inspections : db.Inspections.AsNoTracking();
        return await query.Include(i => i.Rooms).ThenInclude(r => r.Defects)
                   .FirstOrDefaultAsync(i => i.Id == inspectionId && i.CompanyId == companyId, ct)
               ?? throw new NotFoundException(EntityNames.Inspection, inspectionId);
    }

    /// <summary>Loads an inspection assigned to the calling agent (tracked, with rooms and defects).</summary>
    public async Task<Inspection> GetForAssignedAgentAsync(Guid inspectionId, CancellationToken ct, bool tracking = true)
    {
        var agentId = RequireAgent();
        var query = tracking ? db.Inspections : db.Inspections.AsNoTracking();
        return await query.Include(i => i.Rooms).ThenInclude(r => r.Defects)
                   .FirstOrDefaultAsync(i => i.Id == inspectionId && i.AgentId == agentId, ct)
               ?? throw new NotFoundException(EntityNames.Inspection, inspectionId);
    }

    public Guid RequireAgent()
    {
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsInRole(AppRoles.Agent)) throw new ForbiddenException(new("Only agents can perform this action.", "Apenas vistoriadores podem realizar esta ação."));
        return userId;
    }

    public Guid RequireTenant()
    {
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsInRole(AppRoles.Tenant)) throw new ForbiddenException(new("Only tenants can perform this action.", "Apenas inquilinos podem realizar esta ação."));
        return userId;
    }

    /// <summary>Tenants only see inspections of tenancies they joined, and only once the report was sent to them.</summary>
    public IQueryable<Inspection> TenantVisibleInspections(Guid tenantUserId) =>
        db.Inspections.Where(i => i.TenancyId != null &&
                                  db.TenancyMembers.Any(m => m.TenancyId == i.TenancyId && m.UserId == tenantUserId) &&
                                  (i.Status == InspectionStatus.AwaitingTenant || i.Status == InspectionStatus.Accepted ||
                                   i.Status == InspectionStatus.Disputed));

    /// <summary>Who may read the finalized report of <paramref name="inspection"/>.</summary>
    public async Task<InspectionViewerKind> RequireReportViewerAsync(Inspection inspection, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        if (currentUser.IsInRole(AppRoles.Company) && await companyAccess.IsMemberOfAsync(inspection.CompanyId, ct))
            return InspectionViewerKind.Company;
        if (currentUser.IsInRole(AppRoles.Agent) && inspection.AgentId == userId)
            return InspectionViewerKind.Agent;
        if (currentUser.IsInRole(AppRoles.Tenant) &&
            await TenantVisibleInspections(userId).AnyAsync(i => i.Id == inspection.Id, ct))
            return InspectionViewerKind.Tenant;
        throw new NotFoundException(EntityNames.Report);
    }
}

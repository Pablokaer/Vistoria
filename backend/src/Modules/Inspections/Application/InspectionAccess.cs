using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
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
public sealed class InspectionAccess(IAppDbContext db, ICurrentUser currentUser, CompanyAccess companyAccess, IClock clock)
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

    /// <summary>
    /// Loads an inspection assigned to the calling agent (tracked, with rooms and defects). A Team draft already
    /// designated to the agent stays hidden until the company publishes it.
    /// </summary>
    public async Task<Inspection> GetForAssignedAgentAsync(Guid inspectionId, CancellationToken ct, bool tracking = true)
    {
        var agentId = RequireAgent();
        var query = tracking ? db.Inspections : db.Inspections.AsNoTracking();
        return await query.Include(i => i.Rooms).ThenInclude(r => r.Defects)
                   .FirstOrDefaultAsync(i => i.Id == inspectionId && i.AgentId == agentId && i.Status != InspectionStatus.Draft, ct)
               ?? throw new NotFoundException(EntityNames.Inspection, inspectionId);
    }

    /// <summary>
    /// Open, unassigned inspections an agent may see and accept without an invitation: every Public one plus the
    /// Team ones of companies that registered the agent. Example: <c>await access.OpenForAgent(agentId).CountAsync(ct)</c>.
    /// </summary>
    public IQueryable<Inspection> OpenForAgent(Guid agentId)
    {
        var now = clock.UtcNow;
        return db.Inspections.Where(i => i.Status == InspectionStatus.Open && i.AgentId == null && (i.AcceptBy == null || i.AcceptBy > now) &&
            (i.Visibility == InspectionVisibility.Public ||
             (i.Visibility == InspectionVisibility.Team && db.CompanyAgents.Any(a => a.CompanyId == i.CompanyId && a.AgentUserId == agentId))));
    }

    /// <summary>Whether <paramref name="agentId"/> may see <paramref name="inspection"/> before it is assigned to them.</summary>
    public Task<bool> CanAgentSeeAsync(Inspection inspection, Guid agentId, CancellationToken ct) => inspection.Visibility switch
    {
        InspectionVisibility.Private => HasVerifiedInvitationAsync(inspection.Id, agentId, ct),
        InspectionVisibility.Team => IsTeamAgentAsync(inspection.CompanyId, agentId, ct),
        _ => Task.FromResult(true),
    };

    /// <summary>True when the company registered <paramref name="agentId"/> in its team. Example: <c>await access.IsTeamAgentAsync(companyId, agentId, ct)</c>.</summary>
    public Task<bool> IsTeamAgentAsync(Guid companyId, Guid agentId, CancellationToken ct) =>
        db.CompanyAgents.AnyAsync(a => a.CompanyId == companyId && a.AgentUserId == agentId, ct);

    private Task<bool> HasVerifiedInvitationAsync(Guid inspectionId, Guid agentId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.InspectionInvitations.AnyAsync(x => x.InspectionId == inspectionId && x.UsedByUserId == agentId &&
                                                      x.RevokedAt == null && x.ExpiresAt > now, ct);
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

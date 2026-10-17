using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Inspections.Application;

/// <param name="AgentId">A team agent to assign, or null to release the inspection back to the whole team.</param>
public sealed record AssignInspectionRequest(Guid? AgentId);

/// <summary>
/// Company side of Team and Private inspections handed to the company's own agents: direct assignment, release back
/// to the team (Team only), and the notifications that go with them (see ADR 0017 and ADR 0021).
/// </summary>
public sealed class TeamAssignmentService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionDetailsBuilder details,
    INotificationService notifications,
    IAuditLogger audit,
    IClock clock,
    IOptions<AppUrlOptions> urls)
{
    /// <summary>
    /// Assigns (<c>AgentId</c> set) or releases (<c>null</c>) a Team inspection that has not started yet.
    /// Example: <c>await service.SetAssignmentAsync(inspectionId, new(agentId), ct)</c>.
    /// </summary>
    public async Task<InspectionDetailsDto> SetAssignmentAsync(Guid inspectionId, AssignInspectionRequest request, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        if (request.AgentId is { } agentId) await AssignAsync(inspection, agentId, ct);
        else Release(inspection);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(InspectionMessages.ChangedConcurrently); // e.g. the agent started it meanwhile
        }
        return await details.BuildAsync(inspection, InspectionViewerKind.Company, ct);
    }

    /// <summary>Hands the inspection to a registered team agent; a draft only records the choice until it is published.</summary>
    public async Task AssignAsync(Inspection inspection, Guid agentId, CancellationToken ct)
    {
        if (!await access.IsTeamAgentAsync(inspection.CompanyId, agentId, ct))
            throw new ValidationException("AgentId", new(
                $"Agent {agentId} is not registered in your company's team; register the agent first.",
                $"O vistoriador {agentId} não está cadastrado na equipe da sua empresa; cadastre-o primeiro."));
        inspection.AssignToTeamAgent(agentId, clock.UtcNow);
        await RevokeInvitationsAsync(inspection, ct);
        audit.Record(AuditActions.InspectionAssignedToAgent, nameof(Inspection), inspection.Id, new { agentId, status = inspection.Status.ToString() });
        if (inspection.Status == InspectionStatus.Assigned) NotifyAssigned(inspection, agentId);
    }

    /// <summary>
    /// After publishing: tells the designated agent (Team or Private), or every team agent when a Team inspection is
    /// open to the whole team. Private inspections without an agent are announced by their invitation instead.
    /// </summary>
    public async Task NotifyPublishedAsync(Inspection inspection, CancellationToken ct)
    {
        if (inspection.AgentId is { } assigned)
        {
            NotifyAssigned(inspection, assigned);
            return;
        }
        if (inspection.Visibility != InspectionVisibility.Team) return;
        var team = await db.CompanyAgents.AsNoTracking().Where(a => a.CompanyId == inspection.CompanyId).Select(a => a.AgentUserId).ToListAsync(ct);
        foreach (var agentId in team)
            notifications.Enqueue(new NotificationMessage(NotificationTypes.TeamInspectionAvailable, "A new team inspection is available",
                "Your company published an inspection for its team. The first agent to accept it takes it.", RecipientUserId: agentId,
                Link: urls.Value.Web($"/agent/available/{inspection.Id}")));
    }

    /// <summary>
    /// A Private inspection handed to a team agent no longer goes through link + code, so earlier invitations stop
    /// working: an outside inspector holding one can no longer open the inspection.
    /// </summary>
    private async Task RevokeInvitationsAsync(Inspection inspection, CancellationToken ct)
    {
        if (inspection.Visibility != InspectionVisibility.Private) return;
        var now = clock.UtcNow;
        var active = await db.InspectionInvitations.Where(x => x.InspectionId == inspection.Id && x.RevokedAt == null).ToListAsync(ct);
        foreach (var invitation in active) invitation.Revoke(now);
    }

    private void Release(Inspection inspection)
    {
        inspection.ReleaseToTeam(clock.UtcNow);
        audit.Record(AuditActions.InspectionReleasedToTeam, nameof(Inspection), inspection.Id);
    }

    private void NotifyAssigned(Inspection inspection, Guid agentId) =>
        notifications.Enqueue(new NotificationMessage(NotificationTypes.TeamInspectionAssigned, "An inspection was assigned to you",
            "Your company assigned you an inspection. Open it to see the address and start when you are on site.", RecipientUserId: agentId,
            Link: urls.Value.Web($"/agent/inspections/{inspection.Id}")));
}

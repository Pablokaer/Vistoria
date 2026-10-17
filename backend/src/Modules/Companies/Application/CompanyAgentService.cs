using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Companies.Application;

/// <param name="Password">The new agent account's password; the company hands it to the agent.</param>
public sealed record RegisterTeamAgentRequest(string Email, string FullName, string? Password, string? Phone);

/// <param name="ActiveInspections">This company's inspections the agent holds that are Assigned, InProgress or in Review.</param>
public sealed record TeamAgentDto(Guid AgentId, string FullName, string Email, string? Phone, int ActiveInspections, DateTimeOffset AddedAt);

/// <summary>
/// The company's own team of agents: list, register (always a new agent account owned by the company) and remove.
/// Agents only ever see this company's Public inspections and the ones assigned to them (see ADR 0017 and ADR 0022).
/// </summary>
public sealed class CompanyAgentService(
    IAppDbContext db,
    CompanyAccess access,
    UserAccountFactory accounts,
    UserManager<User> userManager,
    INotificationService notifications,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock)
{
    /// <summary>Every member may read the team (needed to pick an agent for an inspection). Example: <c>await service.ListAsync(ct)</c>.</summary>
    public async Task<IReadOnlyList<TeamAgentDto>> ListAsync(CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.View, ct);
        return await ProjectTeam(companyId).ToListAsync(ct);
    }

    /// <summary>
    /// Creates an Agent account with the given password and adds it to the team. Agents cannot sign up themselves and
    /// an existing account is never linked: that would let a company take over someone else's account without consent.
    /// Example: <c>await service.RegisterAsync(new("ana@x.com", "Ana", "Passw0rd!", null), ct)</c>.
    /// </summary>
    public async Task<TeamAgentDto> RegisterAsync(RegisterTeamAgentRequest request, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.ManageMembers, ct);
        ValidateRequest(request);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await EnsureEmailIsFreeAsync(request.Email);
        var agent = await CreateAgentAsync(request);
        var link = new CompanyAgent { Id = Guid.NewGuid(), CompanyId = companyId, AgentUserId = agent.Id, CreatedAt = clock.UtcNow, CreatedBy = currentUser.RequireUserId() };
        db.CompanyAgents.Add(link);
        audit.Record(AuditActions.TeamAgentRegistered, nameof(CompanyAgent), link.Id, new { agentId = agent.Id });
        notifications.Enqueue(new NotificationMessage(NotificationTypes.TeamAgentAdded, "You were added to a company team",
            "Your company registered you as one of its agents. Its open inspections appear under Available.", RecipientUserId: agent.Id));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await ProjectTeam(companyId, agent.Id).FirstAsync(ct);
    }

    /// <summary>
    /// Removes the agent from the team. Inspections already assigned stay with the agent (the company can reassign
    /// those not started yet). Example: <c>await service.RemoveAsync(agentId, ct)</c>.
    /// </summary>
    public async Task RemoveAsync(Guid agentId, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.ManageMembers, ct);
        var link = await db.CompanyAgents.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AgentUserId == agentId, ct)
                   ?? throw new NotFoundException(EntityNames.TeamAgent, agentId);
        db.CompanyAgents.Remove(link);
        audit.Record(AuditActions.TeamAgentRemoved, nameof(CompanyAgent), link.Id, new { agentId });
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateRequest(RegisterTeamAgentRequest request)
    {
        var errors = new Dictionary<string, LocalizedText[]>();
        if (string.IsNullOrWhiteSpace(request.Email)) errors["Email"] = [new("Email is required.", "O e-mail é obrigatório.")];
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 200)
            errors["FullName"] = [new("Full name is required (max 200 characters).", "O nome completo é obrigatório (máximo de 200 caracteres).")];
        if (errors.Count > 0) throw new ValidationException(UserAccountFactory.RegistrationInvalid, errors);
    }

    /// <summary>
    /// The same answer for every existing account (agent, company or tenant, this team or another): the role of an
    /// address is not revealed. Example: <c>await EnsureEmailIsFreeAsync("ana@x.com")</c>.
    /// </summary>
    private async Task EnsureEmailIsFreeAsync(string email)
    {
        if (await userManager.FindByEmailAsync(email.Trim()) is null) return;
        throw new ValidationException("Email", new(
            $"{email.Trim()} is already used by an account; agent accounts need an email of their own.",
            $"{email.Trim()} já é usado por uma conta; contas de vistoriador precisam de um e-mail próprio."));
    }

    private Task<User> CreateAgentAsync(RegisterTeamAgentRequest request)
    {
        if (string.IsNullOrEmpty(request.Password))
            throw new ValidationException("Password", new(
                "Password is required to create a new agent account.", "A senha é obrigatória para criar uma nova conta de vistoriador."));
        return accounts.CreateAsync(new NewUserAccount(request.Email, request.Password, request.FullName, AppRoles.Agent, request.Phone));
    }

    // Filter and order before projecting: EF Core cannot translate member access on a constructor-projected record.
    private IQueryable<TeamAgentDto> ProjectTeam(Guid companyId, Guid? onlyAgentId = null) =>
        from a in db.CompanyAgents.AsNoTracking()
        join u in db.Users.AsNoTracking() on a.AgentUserId equals u.Id
        where a.CompanyId == companyId && (onlyAgentId == null || a.AgentUserId == onlyAgentId)
        orderby u.FullName
        select new TeamAgentDto(u.Id, u.FullName, u.Email!,
            db.AgentProfiles.Where(p => p.UserId == u.Id).Select(p => p.Phone).FirstOrDefault(),
            db.Inspections.Count(i => i.CompanyId == companyId && i.AgentId == u.Id &&
                                      (i.Status == InspectionStatus.Assigned || i.Status == InspectionStatus.InProgress || i.Status == InspectionStatus.Review)),
            a.CreatedAt);
}

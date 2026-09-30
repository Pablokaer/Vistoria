using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Companies.Application;

public sealed record CreateCompanyRequest(string Name, string? ContactEmail, string? Phone);

public sealed record CompanyDto(Guid Id, string Name, string? ContactEmail, string? Phone, string MyRole, DateTimeOffset CreatedAt);

public sealed record RecentInspectionDto(Guid Id, string PropertyAddress, Guid PropertyId, string InspectionType,
    string Status, string? AgentName, DateTimeOffset UpdatedAt);

public sealed record CompanyDashboardDto(
    int Properties,
    int Draft,
    int Open,
    int Assigned,
    int InProgress,
    int InReview,
    int AwaitingTenant,
    int Completed,
    int Disputed,
    IReadOnlyList<RecentInspectionDto> RecentInspections);

public sealed class CompanyService(
    IAppDbContext db,
    ICurrentUser currentUser,
    CompanyAccess access,
    IAuditLogger audit,
    IClock clock)
{
    public async Task<CompanyDto> CreateWorkspaceAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsInRole(AppRoles.Company))
            throw new ForbiddenException("Only company accounts can create a company workspace.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            throw new ValidationException("Name", "Company name is required (max 200 characters).");
        if (await db.CompanyMembers.AnyAsync(m => m.UserId == userId, ct))
            throw new DomainRuleException("company.already_exists", "You already belong to a company workspace.");

        var now = clock.UtcNow;
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            CreatedAt = now,
            CreatedBy = userId,
        };
        company.Members.Add(new CompanyMember
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, UserId = userId, Role = CompanyRole.Owner, CreatedAt = now,
        });
        db.Companies.Add(company);
        audit.Record(AuditActions.CompanyCreated, nameof(Company), company.Id, new { company.Name });
        await db.SaveChangesAsync(ct);
        return new CompanyDto(company.Id, company.Name, company.ContactEmail, company.Phone, nameof(CompanyRole.Owner), now);
    }

    public async Task<CompanyDto> GetMineAsync(CancellationToken ct)
    {
        var membership = await access.GetMembershipAsync(ct);
        var company = await db.Companies.AsNoTracking().FirstAsync(c => c.Id == membership.CompanyId, ct);
        return new CompanyDto(company.Id, company.Name, company.ContactEmail, company.Phone, membership.Role.ToString(), company.CreatedAt);
    }

    public async Task<CompanyDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.View, ct);
        var properties = await db.Properties.CountAsync(p => p.CompanyId == companyId, ct);
        var counts = await db.Inspections.Where(i => i.CompanyId == companyId)
            .GroupBy(i => i.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int C(params InspectionStatus[] s) => counts.Where(x => s.Contains(x.Key)).Sum(x => x.Count);

        var recent = await (from i in db.Inspections.AsNoTracking()
                            join p in db.Properties.AsNoTracking() on i.PropertyId equals p.Id
                            join u in db.Users.AsNoTracking() on i.AgentId equals u.Id into agents
                            from agent in agents.DefaultIfEmpty()
                            where i.CompanyId == companyId
                            orderby i.UpdatedAt descending
                            select new { i.Id, p.AddressLine1, p.City, p.Postcode, i.PropertyId, i.InspectionType, i.Status, AgentName = agent != null ? agent.FullName : null, i.UpdatedAt })
            .Take(10).ToListAsync(ct);

        return new CompanyDashboardDto(
            properties,
            C(InspectionStatus.Draft),
            C(InspectionStatus.Open),
            C(InspectionStatus.Assigned),
            C(InspectionStatus.InProgress),
            C(InspectionStatus.Review),
            C(InspectionStatus.AwaitingTenant),
            C(InspectionStatus.Completed, InspectionStatus.Accepted),
            C(InspectionStatus.Disputed),
            recent.Select(r => new RecentInspectionDto(r.Id, $"{r.AddressLine1}, {r.City} {r.Postcode}", r.PropertyId,
                r.InspectionType.ToString(), r.Status.ToString(), r.AgentName, r.UpdatedAt)).ToList());
    }
}

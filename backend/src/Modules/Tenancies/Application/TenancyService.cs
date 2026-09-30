using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Tenancies.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Tenancies.Application;

public sealed record CreateTenancyRequest(Guid PropertyId, DateOnly StartDate, DateOnly? EndDate, string? Reference);

public sealed record UpdateTenancyRequest(DateOnly StartDate, DateOnly? EndDate, string? Reference, TenancyStatus Status);

public sealed record InviteTenantRequest(string Email, string FullName);

public sealed record TenancyMemberDto(Guid Id, string FullName, string Email, bool Joined, DateTimeOffset InvitedAt, DateTimeOffset? JoinedAt);

public sealed record TenancyDto(Guid Id, Guid PropertyId, string? Reference, DateOnly StartDate, DateOnly? EndDate,
    string Status, IReadOnlyList<TenancyMemberDto> Members, DateTimeOffset CreatedAt);

/// <summary>Returned once to the company; the token itself is stored only as a hash.</summary>
public sealed record TenantInvitationDto(TenancyMemberDto Member, string InvitationLink, DateTimeOffset ExpiresAt);

public sealed record TenancyInvitationPreviewDto(string PropertyAddress, string CompanyName, string InvitedEmail, DateTimeOffset ExpiresAt);

public sealed class TenancyService(
    IAppDbContext db,
    CompanyAccess access,
    ICurrentUser currentUser,
    IAuditLogger audit,
    INotificationService notifications,
    IClock clock,
    IOptions<AppUrlOptions> urls,
    IOptions<InspectionRulesOptions> rules)
{
    public async Task<IReadOnlyList<TenancyDto>> ListForPropertyAsync(Guid propertyId, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.View, ct);
        var tenancies = await db.Tenancies.AsNoTracking().Include(t => t.Members)
            .Where(t => t.PropertyId == propertyId && t.CompanyId == companyId)
            .OrderByDescending(t => t.StartDate).ToListAsync(ct);
        return tenancies.Select(ToDto).ToList();
    }

    public async Task<TenancyDto> GetAsync(Guid tenancyId, CancellationToken ct) => ToDto(await LoadAsync(tenancyId, CompanyPermission.View, ct));

    public async Task<TenancyDto> CreateAsync(CreateTenancyRequest request, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.ManageProperties, ct);
        var propertyExists = await db.Properties.AnyAsync(p => p.Id == request.PropertyId && p.CompanyId == companyId, ct);
        if (!propertyExists) throw new NotFoundException("Property", request.PropertyId);

        var tenancy = Tenancy.Create(companyId, request.PropertyId, request.StartDate, request.EndDate, request.Reference, clock.UtcNow);
        db.Tenancies.Add(tenancy);
        audit.Record(AuditActions.TenancyCreated, nameof(Tenancy), tenancy.Id, new { propertyId = request.PropertyId });
        await db.SaveChangesAsync(ct);
        return ToDto(tenancy);
    }

    public async Task<TenancyDto> UpdateAsync(Guid tenancyId, UpdateTenancyRequest request, CancellationToken ct)
    {
        var tenancy = await LoadAsync(tenancyId, CompanyPermission.ManageProperties, ct, tracking: true);
        var now = clock.UtcNow;
        tenancy.SetDates(request.StartDate, request.EndDate, now);
        tenancy.Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
        if (request.Status != tenancy.Status) tenancy.ChangeStatus(request.Status, now);
        await db.SaveChangesAsync(ct);
        return ToDto(tenancy);
    }

    public async Task<TenantInvitationDto> InviteTenantAsync(Guid tenancyId, InviteTenantRequest request, CancellationToken ct)
    {
        var tenancy = await LoadAsync(tenancyId, CompanyPermission.ManageProperties, ct, tracking: true);
        var email = request.Email?.Trim() ?? string.Empty;
        if (!email.Contains('@', StringComparison.Ordinal) || email.Length > 256)
            throw new ValidationException("Email", "A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ValidationException("FullName", "Tenant name is required.");
        if (tenancy.Members.Any(m => string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Email", "This tenant is already part of the tenancy.");

        var now = clock.UtcNow;
        var token = SecureTokens.Create();
        var member = new TenancyMember
        {
            Id = Guid.NewGuid(),
            TenancyId = tenancy.Id,
            Email = email,
            FullName = request.FullName.Trim(),
            InvitationTokenHash = SecureTokens.Sha256Hex(token),
            InvitationExpiresAt = now.AddDays(rules.Value.TenantInvitationDays),
            InvitedAt = now,
        };
        db.TenancyMembers.Add(member);

        var link = urls.Value.Web($"/tenant/invite/{token}");
        notifications.Enqueue(new NotificationMessage(NotificationTypes.TenancyInvitation,
            "You have been invited to review your property inspection",
            $"Hello {member.FullName}, please create an account or sign in with {email} to access your tenancy's inspection reports.",
            RecipientEmail: email, TransientLink: link));
        audit.Record(AuditActions.TenantInvited, nameof(Tenancy), tenancy.Id, new { memberId = member.Id });
        await db.SaveChangesAsync(ct);
        return new TenantInvitationDto(ToMemberDto(member), link, member.InvitationExpiresAt!.Value);
    }

    public async Task<TenancyInvitationPreviewDto> PreviewInvitationAsync(string token, CancellationToken ct)
    {
        var member = await FindInvitationAsync(token, ct);
        var info = await (from t in db.Tenancies.AsNoTracking()
                          join p in db.Properties.AsNoTracking() on t.PropertyId equals p.Id
                          join c in db.Companies.AsNoTracking() on t.CompanyId equals c.Id
                          where t.Id == member.TenancyId
                          select new { p.AddressLine1, p.City, c.Name }).FirstAsync(ct);
        return new TenancyInvitationPreviewDto($"{info.AddressLine1}, {info.City}", info.Name, MaskEmail(member.Email),
            member.InvitationExpiresAt!.Value);
    }

    /// <summary>Links the signed-in tenant to the tenancy. The account email must match the invited email.</summary>
    public async Task<TenancyMemberDto> AcceptInvitationAsync(string token, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsInRole(AppRoles.Tenant))
            throw new ForbiddenException("Sign in with a tenant account to accept this invitation.");
        var member = await FindInvitationAsync(token, ct, tracking: true);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        if (!string.Equals(user.Email, member.Email, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("This invitation was sent to a different email address.");

        var now = clock.UtcNow;
        member.UserId = userId;
        member.JoinedAt = now;
        member.InvitationTokenHash = null;
        audit.Record(AuditActions.TenantJoinedTenancy, nameof(Tenancy), member.TenancyId, new { memberId = member.Id });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This invitation was just used. Refresh and try again.");
        }
        return ToMemberDto(member);
    }

    private async Task<TenancyMember> FindInvitationAsync(string token, CancellationToken ct, bool tracking = false)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new NotFoundException("Invitation");
        var hash = SecureTokens.Sha256Hex(token);
        var query = tracking ? db.TenancyMembers : db.TenancyMembers.AsNoTracking();
        var member = await query.FirstOrDefaultAsync(m => m.InvitationTokenHash == hash, ct);
        if (member is null || member.UserId is not null || member.InvitationExpiresAt <= clock.UtcNow)
            throw new NotFoundException("Invitation");
        return member;
    }

    private async Task<Tenancy> LoadAsync(Guid tenancyId, CompanyPermission permission, CancellationToken ct, bool tracking = false)
    {
        var companyId = await access.RequireAsync(permission, ct);
        var query = tracking ? db.Tenancies.Include(t => t.Members) : db.Tenancies.AsNoTracking().Include(t => t.Members);
        return await query.FirstOrDefaultAsync(t => t.Id == tenancyId && t.CompanyId == companyId, ct)
               ?? throw new NotFoundException("Tenancy", tenancyId);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 1 ? email : $"{email[0]}***{email[(at - 1)..]}";
    }

    public static TenancyMemberDto ToMemberDto(TenancyMember m) =>
        new(m.Id, m.FullName, m.Email, m.UserId is not null, m.InvitedAt, m.JoinedAt);

    public static TenancyDto ToDto(Tenancy t) => new(t.Id, t.PropertyId, t.Reference, t.StartDate, t.EndDate,
        t.Status.ToString(), t.Members.OrderBy(m => m.InvitedAt).Select(ToMemberDto).ToList(), t.CreatedAt);
}

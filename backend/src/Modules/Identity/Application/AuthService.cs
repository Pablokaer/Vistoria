using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Tenants.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Identity.Application;

public sealed class AuthService(
    IAppDbContext db,
    UserManager<User> userManager,
    ITokenService tokens,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock,
    SubscriptionAccessService subscriptions,
    IOptions<AuthOptions> options)
{
    private readonly AuthOptions _options = options.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Email)) errors["Email"] = ["Email is required."];
        if (string.IsNullOrWhiteSpace(request.FullName)) errors["FullName"] = ["Full name is required."];
        if (string.IsNullOrEmpty(request.Password)) errors["Password"] = ["Password is required."];
        if (!AppRoles.IsSelfRegistrable(request.Role)) errors["Role"] = ["Role must be Company, Agent or Tenant."];
        if (errors.Count > 0) throw new ValidationException("Registration is invalid.", errors);

        var now = clock.UtcNow;
        var email = request.Email.Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            CreatedAt = now,
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw ToValidation(result);

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded) throw ToValidation(roleResult);

        if (request.Role == AppRoles.Agent)
            db.AgentProfiles.Add(new AgentProfile { UserId = user.Id, DisplayName = user.FullName, CreatedAt = now });
        if (request.Role == AppRoles.Tenant)
            db.TenantProfiles.Add(new TenantProfile { UserId = user.Id, CreatedAt = now });

        audit.Record(AuditActions.UserRegistered, nameof(User), user.Id, new { role = request.Role }, user.Id);
        var auth = await IssueTokensAsync(user, familyId: null, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return auth;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        const string invalid = "Invalid email or password.";
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) throw new UnauthorizedException(invalid);
        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("Account temporarily locked after repeated failed sign-ins. Try again later.");
        if (!await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedException(invalid);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        audit.Record(AuditActions.UserLoggedIn, nameof(User), user.Id, userId: user.Id);
        var auth = await IssueTokensAsync(user, familyId: null, ct);
        await db.SaveChangesAsync(ct);
        return auth;
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new UnauthorizedException("Refresh token missing.");
        var now = clock.UtcNow;
        var hash = SecureTokens.Sha256Hex(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                       ?? throw new UnauthorizedException("Invalid refresh token.");

        if (existing.RevokedAt is not null)
        {
            // A rotated token presented again: benign if it was just rotated (parallel tabs), otherwise treat as theft.
            var justRotated = existing.ReplacedByTokenId is not null && now - existing.RevokedAt < TimeSpan.FromSeconds(30);
            if (!justRotated)
            {
                await db.RefreshTokens.Where(t => t.FamilyId == existing.FamilyId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
            }
            throw new UnauthorizedException("Refresh token is no longer valid.");
        }
        if (existing.ExpiresAt <= now) throw new UnauthorizedException("Refresh token expired.");

        // Atomically claim the token so two concurrent refreshes cannot both rotate it.
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == existing.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        if (claimed == 0) throw new UnauthorizedException("Refresh token is no longer valid.");

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
                   ?? throw new UnauthorizedException("Invalid refresh token.");
        var auth = await IssueTokensAsync(user, existing.FamilyId, ct, out var newTokenId);
        await db.SaveChangesAsync(ct);
        await db.RefreshTokens.Where(t => t.Id == existing.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReplacedByTokenId, newTokenId), ct);
        return auth;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = SecureTokens.Sha256Hex(refreshToken);
        var now = clock.UtcNow;
        await db.RefreshTokens.Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public async Task<MeResponse> MeAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException();
        return await BuildMeAsync(user, ct);
    }

    private Task<AuthResult> IssueTokensAsync(User user, Guid? familyId, CancellationToken ct) =>
        IssueTokensAsync(user, familyId, ct, out _);

    private Task<AuthResult> IssueTokensAsync(User user, Guid? familyId, CancellationToken ct, out Guid refreshTokenId)
    {
        var now = clock.UtcNow;
        var raw = SecureTokens.Create(48);
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = SecureTokens.Sha256Hex(raw),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
            CreatedByIp = currentUser.IpAddress,
        };
        db.RefreshTokens.Add(entity);
        refreshTokenId = entity.Id;
        return BuildAsync();

        async Task<AuthResult> BuildAsync()
        {
            var me = await BuildMeAsync(user, ct);
            var access = tokens.CreateAccessToken(user, me.Roles);
            return new AuthResult(access.Token, access.ExpiresAt, raw, entity.ExpiresAt, me);
        }
    }

    private async Task<MeResponse> BuildMeAsync(User user, CancellationToken ct)
    {
        var roles = await db.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name!).ToListAsync(ct);
        var company = await (from m in db.CompanyMembers.AsNoTracking()
                             join c in db.Companies.AsNoTracking() on m.CompanyId equals c.Id
                             where m.UserId == user.Id
                             orderby m.CreatedAt
                             select new CompanyMembershipDto(c.Id, c.Name, m.Role.ToString()))
            .FirstOrDefaultAsync(ct);
        var subscription = await subscriptions.GetSummaryAsync(user.Id, roles, ct);
        return new MeResponse(user.Id, user.Email!, user.FullName, roles, company, subscription);
    }

    private static ValidationException ToValidation(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code.Contains("Password", StringComparison.Ordinal) ? "Password"
                : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "Email" : "General")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Code is "DuplicateUserName" or "DuplicateEmail"
                ? "An account with this email already exists." : e.Description).Distinct().ToArray());
        return new ValidationException("Registration is invalid.", errors);
    }
}

using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Tenancies.Application;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Identity.Application;

public sealed class AuthService(
    IAppDbContext db,
    UserManager<User> userManager,
    UserAccountFactory accounts,
    TenancyInvitationLookup tenantInvitations,
    ITokenService tokens,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock,
    SubscriptionAccessService subscriptions,
    IOptions<AuthOptions> options)
{
    private readonly AuthOptions _options = options.Value;

    /// <summary>
    /// 401 code for a refresh token that another request (usually a parallel tab) rotated a moment ago. The session is
    /// still alive, so callers must not treat it as signed out (e.g. the web session hint stays; see ADR 0018).
    /// </summary>
    public const string RefreshSupersededCode = "auth.refresh_superseded";

    /// <summary>
    /// Self-registration of a Company, or of a Tenant from the invitation it was sent (the tenant joins that tenancy in
    /// the same transaction). Example: <c>await auth.RegisterAsync(new("tom@x.com", "Passw0rd!", "Tom", "Tenant", token), ct)</c>.
    /// </summary>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        ValidateRegistration(request);
        var invitation = request.Role == AppRoles.Tenant
            ? await tenantInvitations.RequireForSignUpAsync(request.InvitationToken, request.Email, ct)
            : null;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = await accounts.CreateAsync(new NewUserAccount(request.Email, request.Password, request.FullName, request.Role));
        if (invitation is not null) tenantInvitations.Join(invitation, user.Id);

        audit.Record(AuditActions.UserRegistered, nameof(User), user.Id, new { role = request.Role }, user.Id);
        var auth = await IssueTokensAsync(user, familyId: null, ct);
        await SaveRegistrationAsync(ct);
        await tx.CommitAsync(ct);
        return auth;
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        var errors = new Dictionary<string, LocalizedText[]>();
        if (string.IsNullOrWhiteSpace(request.Email)) errors["Email"] = [new("Email is required.", "O e-mail é obrigatório.")];
        if (string.IsNullOrWhiteSpace(request.FullName)) errors["FullName"] = [new("Full name is required.", "O nome completo é obrigatório.")];
        if (string.IsNullOrEmpty(request.Password)) errors["Password"] = [new("Password is required.", "A senha é obrigatória.")];
        if (!AppRoles.IsSelfRegistrable(request.Role))
            errors["Role"] = [new($"Role must be Company or Tenant (got '{request.Role}'); agent accounts are created by their company.",
                $"O perfil deve ser Company ou Tenant (recebido: '{request.Role}'); contas de vistoriador são criadas pela empresa.")];
        if (errors.Count > 0) throw new ValidationException(RegistrationInvalid, errors);
    }

    /// <summary>The invitation's row version makes two sign-ups from the same link race-safe: the second one gets 409.</summary>
    private async Task SaveRegistrationAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(new("This invitation was just used. Sign in instead.", "Este convite acabou de ser utilizado. Entre com a sua conta."));
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var invalid = new LocalizedText("Invalid email or password.", "E-mail ou senha inválidos.");
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) throw new UnauthorizedException(invalid);
        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException(new("Account temporarily locked after repeated failed sign-ins. Try again later.",
                "Conta bloqueada temporariamente após várias tentativas de acesso sem sucesso. Tente novamente mais tarde."));
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
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new UnauthorizedException(new("Refresh token missing.", "Token de renovação ausente."));
        var now = clock.UtcNow;
        var hash = SecureTokens.Sha256Hex(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                       ?? throw new UnauthorizedException(InvalidRefreshToken);

        if (existing.RevokedAt is not null)
        {
            // A rotated token presented again: benign if it was just rotated (parallel tabs), otherwise treat as theft.
            var justRotated = existing.ReplacedByTokenId is not null && now - existing.RevokedAt < TimeSpan.FromSeconds(30);
            if (!justRotated)
            {
                await db.RefreshTokens.Where(t => t.FamilyId == existing.FamilyId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
            }
            throw new UnauthorizedException(RefreshTokenNoLongerValid, justRotated ? RefreshSupersededCode : "unauthorized");
        }
        if (existing.ExpiresAt <= now) throw new UnauthorizedException(new("Refresh token expired.", "O token de renovação expirou."));

        // Atomically claim the token so two concurrent refreshes cannot both rotate it.
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == existing.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        if (claimed == 0) throw new UnauthorizedException(RefreshTokenNoLongerValid, RefreshSupersededCode); // a concurrent refresh won

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
                   ?? throw new UnauthorizedException(InvalidRefreshToken);
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
                             select new CompanyMembershipDto(c.Id, c.Name, m.Role.ToString(), c.ReportLanguage))
            .FirstOrDefaultAsync(ct);
        var subscription = await subscriptions.GetSummaryAsync(user.Id, roles, ct);
        return new MeResponse(user.Id, user.Email!, user.FullName, roles, company, subscription);
    }

    private static readonly LocalizedText RegistrationInvalid = UserAccountFactory.RegistrationInvalid;
    private static readonly LocalizedText InvalidRefreshToken = new("Invalid refresh token.", "Token de renovação inválido.");
    private static readonly LocalizedText RefreshTokenNoLongerValid = new("Refresh token is no longer valid.", "O token de renovação não é mais válido.");
}

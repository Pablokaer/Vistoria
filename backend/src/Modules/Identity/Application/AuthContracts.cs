using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Identity.Domain;

namespace InspectFlow.Modules.Identity.Application;

public sealed record RegisterRequest(string Email, string Password, string FullName, string Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record CompanyMembershipDto(Guid CompanyId, string CompanyName, string Role, string ReportLanguage);

public sealed record MeResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    CompanyMembershipDto? Company,
    SubscriptionSummaryDto Subscription);

public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    MeResponse User);

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyList<string> roles);
}

public sealed class AuthOptions
{
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}

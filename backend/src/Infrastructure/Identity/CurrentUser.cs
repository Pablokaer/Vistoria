using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InspectFlow.Shared.Auth;
using Microsoft.AspNetCore.Http;

namespace InspectFlow.Infrastructure.Identity;

/// <summary>
/// Current user from the validated JWT principal. Background jobs and the development seeder can
/// run as a specific user via <see cref="RunAs"/> (scoped, never reachable from HTTP input).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private (Guid Id, HashSet<string> Roles)? _override;

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public void RunAs(Guid userId, params string[] roles) => _override = (userId, roles.ToHashSet(StringComparer.Ordinal));

    public Guid? UserId
    {
        get
        {
            if (_override is { } o) return o.Id;
            var sub = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _override is not null || Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => _override is { } o ? o.Roles.Contains(role) : Principal?.IsInRole(role) == true;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

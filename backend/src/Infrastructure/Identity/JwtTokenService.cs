using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InspectFlow.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth, IClock clock) : ITokenService
{
    public AccessToken CreateAccessToken(User user, IReadOnlyList<string> roles)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(auth.Value.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.FullName),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Secret));
        var token = new JwtSecurityToken(
            issuer: jwt.Value.Issuer,
            audience: jwt.Value.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

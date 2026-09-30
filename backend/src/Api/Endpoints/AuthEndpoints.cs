using InspectFlow.Api.Infrastructure;
using InspectFlow.Modules.Identity.Application;

namespace InspectFlow.Api.Endpoints;

public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string? RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt, MeResponse User);

/// <summary>
/// Web clients (header X-Client: web) receive the refresh token only as an HttpOnly, SameSite=Strict cookie.
/// Native/mobile clients receive it in the body and send it back in the refresh request body.
/// </summary>
public static class AuthEndpoints
{
    public const string RefreshCookie = "if_refresh";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/register", async (RegisterRequest req, AuthService auth, HttpContext http, CancellationToken ct) =>
            Respond(http, await auth.RegisterAsync(req, ct))).RequireRateLimiting(RateLimits.Auth);

        g.MapPost("/login", async (LoginRequest req, AuthService auth, HttpContext http, CancellationToken ct) =>
            Respond(http, await auth.LoginAsync(req, ct))).RequireRateLimiting(RateLimits.Auth);

        g.MapPost("/refresh", async (RefreshRequest? req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var token = req?.RefreshToken ?? http.Request.Cookies[RefreshCookie];
            return Respond(http, await auth.RefreshAsync(token, ct));
        }).RequireRateLimiting(RateLimits.Refresh);

        g.MapPost("/logout", async (RefreshRequest? req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            await auth.LogoutAsync(req?.RefreshToken ?? http.Request.Cookies[RefreshCookie], ct);
            http.Response.Cookies.Delete(RefreshCookie, CookieOptions(http, DateTimeOffset.UnixEpoch));
            return Results.NoContent();
        });

        g.MapGet("/me", (AuthService auth, CancellationToken ct) => auth.MeAsync(ct)).RequireAuthorization();
    }

    private static IResult Respond(HttpContext http, AuthResult result)
    {
        var isWeb = string.Equals(http.Request.Headers["X-Client"], "web", StringComparison.OrdinalIgnoreCase);
        http.Response.Cookies.Append(RefreshCookie, result.RefreshToken, CookieOptions(http, result.RefreshTokenExpiresAt));
        return Results.Ok(new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, isWeb ? null : result.RefreshToken,
            result.RefreshTokenExpiresAt, result.User));
    }

    private static CookieOptions CookieOptions(HttpContext http, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps || !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expires,
        IsEssential = true,
    };
}

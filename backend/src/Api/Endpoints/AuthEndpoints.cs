using InspectFlow.Api.Infrastructure;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Shared.Errors;

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

    /// <summary>
    /// Holds no secret, only "a session probably exists". The refresh cookie is scoped to /api/auth, so the web server
    /// cannot see it on page requests; this hint lets it send signed-in visitors of "/" to their dashboard without
    /// first rendering the landing page, and lets anonymous visitors skip the refresh call. Same lifetime as the session;
    /// removed on logout and when a refresh fails (see ADR 0018).
    /// </summary>
    public const string SessionHintCookie = "if_session";

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
            try
            {
                return Respond(http, await auth.RefreshAsync(token, ct));
            }
            catch (UnauthorizedException)
            {
                // Expired or revoked session: the next page load must not wait for a dashboard. Registered for when the
                // 401 is written, because the error handler clears the headers set so far.
                http.Response.OnStarting(() =>
                {
                    ClearSessionHint(http);
                    return Task.CompletedTask;
                });
                throw;
            }
        }).RequireRateLimiting(RateLimits.Refresh);

        g.MapPost("/logout", async (RefreshRequest? req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            await auth.LogoutAsync(req?.RefreshToken ?? http.Request.Cookies[RefreshCookie], ct);
            http.Response.Cookies.Delete(RefreshCookie, CookieOptions(http, DateTimeOffset.UnixEpoch));
            ClearSessionHint(http);
            return Results.NoContent();
        });

        g.MapGet("/me", (AuthService auth, CancellationToken ct) => auth.MeAsync(ct)).RequireAuthorization();
    }

    internal static IResult Respond(HttpContext http, AuthResult result)
    {
        var isWeb = string.Equals(http.Request.Headers["X-Client"], "web", StringComparison.OrdinalIgnoreCase);
        http.Response.Cookies.Append(RefreshCookie, result.RefreshToken, CookieOptions(http, result.RefreshTokenExpiresAt));
        http.Response.Cookies.Append(SessionHintCookie, "1", SessionHintOptions(http, result.RefreshTokenExpiresAt));
        return Results.Ok(new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, isWeb ? null : result.RefreshToken,
            result.RefreshTokenExpiresAt, result.User));
    }

    private static void ClearSessionHint(HttpContext http) =>
        http.Response.Cookies.Delete(SessionHintCookie, SessionHintOptions(http, DateTimeOffset.UnixEpoch));

    private static bool SecureCookies(HttpContext http) =>
        http.Request.IsHttps || !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

    private static CookieOptions CookieOptions(HttpContext http, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = SecureCookies(http),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expires,
        IsEssential = true,
    };

    // Lax (not Strict): a signed-in user following a link from an email to "/" must still be recognised.
    private static CookieOptions SessionHintOptions(HttpContext http, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = SecureCookies(http),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expires,
        IsEssential = true,
    };
}

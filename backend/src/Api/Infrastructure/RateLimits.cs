using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.Json;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.RateLimiting;

namespace InspectFlow.Api.Infrastructure;

public static class RateLimits
{
    public const string Auth = "auth";
    public const string Refresh = "refresh";
    public const string Invitation = "invitation";
    public const string Public = "public";
    public const string Upload = "upload";
    public const string Ai = "ai";

    private static readonly LocalizedText TooManyRequests =
        new("Too many requests. Please wait a moment and try again.", "Muitas solicitações. Aguarde um momento e tente novamente.");

    /// <summary>Limits are configurable (RateLimiting:*) so automated tests can raise them.</summary>
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration config) => services.AddRateLimiter(o =>
    {
        var auth = config.GetValue("RateLimiting:AuthPerMinute", 30);
        var invitation = config.GetValue("RateLimiting:InvitationPerFiveMinutes", 10);
        var pub = config.GetValue("RateLimiting:PublicPerMinute", 60);
        var refresh = config.GetValue("RateLimiting:RefreshPerMinute", 120);
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.AddPolicy(Auth, http => Fixed(ByIp(http), auth, TimeSpan.FromMinutes(1)));
        // Token refresh happens on every page load; it must not share the strict sign-in budget.
        o.AddPolicy(Refresh, http => Fixed(ByIp(http), refresh, TimeSpan.FromMinutes(1)));
        o.AddPolicy(Invitation, http => Fixed(ByUserOrIp(http), invitation, TimeSpan.FromMinutes(5)));
        o.AddPolicy(Public, http => Fixed(ByIp(http), pub, TimeSpan.FromMinutes(1)));
        o.AddPolicy(Upload, http => Fixed(ByUserOrIp(http), 120, TimeSpan.FromMinutes(1)));
        o.AddPolicy(Ai, http => Fixed(ByUserOrIp(http), 30, TimeSpan.FromMinutes(1)));
        o.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.ContentType = "application/problem+json";
            var title = TooManyRequests.In(RequestLanguage.Of(ctx.HttpContext));
            await ctx.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(new { status = 429, title, code = "rate_limited" }), ct);
        };
    });

    private static RateLimitPartition<string> Fixed(string key, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            QueueLimit = 0,
        });

    private static string ByIp(HttpContext http) => "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static string ByUserOrIp(HttpContext http) =>
        http.User.FindFirstValue("sub") is { } sub ? "user:" + sub : ByIp(http);
}

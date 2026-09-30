using InspectFlow.Api.Infrastructure;
using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Infrastructure.Storage;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Api.Endpoints;

public static class SharedEndpoints
{
    public static void MapSharedEndpoints(this IEndpointRouteBuilder app)
    {
        // Report of an inspection for any authorized viewer (company member, assigned agent, tenant of the tenancy).
        app.MapGet("/api/inspections/{id:guid}/report", (Guid id, ReportService s, CancellationToken ct) => s.GetForInspectionAsync(id, ct))
            .RequireAuthorization().WithTags("Reports");
        app.MapGet("/api/reports/{id:guid}", (Guid id, ReportService s, CancellationToken ct) => s.GetAsync(id, ct))
            .RequireAuthorization().WithTags("Reports");
        app.MapGet("/api/shared/reports/{token}", (string token, ReportService s, CancellationToken ct) => s.GetSharedAsync(token, ct))
            .RequireRateLimiting(RateLimits.Public).WithTags("Reports");

        app.MapGet("/api/ai/status", (AiAnalysisService s) => s.GetStatus()).RequireAuthorization().WithTags("AI");

        app.MapGet("/api/notifications", async (AppDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            return await db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt).Take(30)
                .Select(n => new { n.Id, n.Type, n.Subject, n.Body, n.Link, n.CreatedAt, n.ReadAt }).ToListAsync(ct);
        }).RequireAuthorization().WithTags("Notifications");

        // Signed, expiring file URLs (local storage). The signature is the authorization: it is only issued
        // after the API checked the caller may see the photo/PDF.
        app.MapGet("/api/files/{**key}", async (string key, long exp, string? sig, UrlSigner signer, IStorageService storage,
            IClock clock, HttpContext http, CancellationToken ct) =>
        {
            if (!StorageKeys.IsValid(key) || !signer.IsValid(key, exp, sig, clock.UtcNow)) return Results.NotFound();
            Stream stream;
            try
            {
                stream = await storage.OpenReadAsync(key, ct);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            http.Response.Headers.CacheControl = "private, max-age=600";
            var contentType = Path.GetExtension(key).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream",
            };
            return Results.Stream(stream, contentType, enableRangeProcessing: true);
        }).WithTags("Files");

        app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
            await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503)).WithTags("Health");
    }
}

public static class Policies
{
    public const string Company = AppRoles.Company;
    public const string Agent = AppRoles.Agent;
    public const string Tenant = AppRoles.Tenant;
}

using InspectFlow.Api.Infrastructure;
using InspectFlow.Modules.Tenancies.Application;
using InspectFlow.Modules.Tenants.Application;

namespace InspectFlow.Api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        // Invitation preview is public (the tenant may not have an account yet), but rate limited.
        app.MapGet("/api/tenant/invitations/{token}", (string token, TenancyService s, CancellationToken ct) => s.PreviewInvitationAsync(token, ct))
            .RequireRateLimiting(RateLimits.Public).WithTags("Tenant");

        var t = app.MapGroup("/api/tenant").RequireAuthorization(Policies.Tenant).WithTags("Tenant");
        t.MapPost("/invitations/{token}/accept", (string token, TenancyService s, CancellationToken ct) => s.AcceptInvitationAsync(token, ct))
            .RequireRateLimiting(RateLimits.Invitation);
        t.MapGet("/dashboard", (TenantService s, CancellationToken ct) => s.DashboardAsync(ct));
        t.MapGet("/inspections/{id:guid}/report", (Guid id, TenantService s, CancellationToken ct) => s.GetReportAsync(id, ct));
        t.MapPost("/inspections/{id:guid}/observations", (Guid id, AddObservationRequest req, TenantService s, CancellationToken ct) => s.AddObservationAsync(id, req, ct));
        t.MapPost("/inspections/{id:guid}/accept", (Guid id, TenantDecisionRequest? req, TenantService s, CancellationToken ct) =>
            s.AcceptAsync(id, req ?? new TenantDecisionRequest(null), ct));
        t.MapPost("/inspections/{id:guid}/dispute", (Guid id, TenantDecisionRequest req, TenantService s, CancellationToken ct) => s.DisputeAsync(id, req, ct));
    }
}

using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Properties.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Modules.Tenancies.Application;

namespace InspectFlow.Api.Endpoints;

public sealed record PublishRequest(string? InviteEmail);

public sealed record ShareLinkRequest(int? Days);

public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(Policies.Company).WithTags("Company");

        api.MapPost("/companies", (CreateCompanyRequest req, CompanyService s, CancellationToken ct) => s.CreateWorkspaceAsync(req, ct));
        api.MapGet("/companies/me", (CompanyService s, CancellationToken ct) => s.GetMineAsync(ct));
        api.MapPut("/companies/me/report-language", (UpdateReportLanguageRequest req, CompanyService s, CancellationToken ct) => s.UpdateReportLanguageAsync(req, ct));
        api.MapGet("/company/dashboard", (CompanyService s, CancellationToken ct) => s.GetDashboardAsync(ct));

        // Properties & rooms
        api.MapGet("/properties", (string? search, PropertyService s, CancellationToken ct) => s.ListAsync(search, ct));
        api.MapPost("/properties", (CreatePropertyRequest req, PropertyService s, CancellationToken ct) => s.CreateAsync(req, ct));
        api.MapGet("/properties/{id:guid}", (Guid id, PropertyService s, CancellationToken ct) => s.GetAsync(id, ct));
        api.MapPut("/properties/{id:guid}", (Guid id, UpdatePropertyRequest req, PropertyService s, CancellationToken ct) => s.UpdateAsync(id, req, ct));
        api.MapPost("/properties/{id:guid}/rooms", (Guid id, RoomInput req, PropertyService s, CancellationToken ct) => s.AddRoomAsync(id, req, ct));
        api.MapPut("/properties/{id:guid}/rooms/order", (Guid id, ReorderRoomsRequest req, PropertyService s, CancellationToken ct) => s.ReorderRoomsAsync(id, req, ct));
        api.MapPut("/properties/{id:guid}/rooms/{roomId:guid}", (Guid id, Guid roomId, RoomInput req, PropertyService s, CancellationToken ct) => s.UpdateRoomAsync(id, roomId, req, ct));
        api.MapDelete("/properties/{id:guid}/rooms/{roomId:guid}", (Guid id, Guid roomId, PropertyService s, CancellationToken ct) => s.ArchiveRoomAsync(id, roomId, ct));
        api.MapGet("/properties/{id:guid}/tenancies", (Guid id, TenancyService s, CancellationToken ct) => s.ListForPropertyAsync(id, ct));
        api.MapGet("/properties/{id:guid}/comparison-candidates", (Guid id, InspectionService s, CancellationToken ct) => s.ListComparisonCandidatesAsync(id, ct));

        // Tenancies
        api.MapPost("/tenancies", (CreateTenancyRequest req, TenancyService s, CancellationToken ct) => s.CreateAsync(req, ct));
        api.MapGet("/tenancies/{id:guid}", (Guid id, TenancyService s, CancellationToken ct) => s.GetAsync(id, ct));
        api.MapPut("/tenancies/{id:guid}", (Guid id, UpdateTenancyRequest req, TenancyService s, CancellationToken ct) => s.UpdateAsync(id, req, ct));
        api.MapPost("/tenancies/{id:guid}/tenants", (Guid id, InviteTenantRequest req, TenancyService s, CancellationToken ct) => s.InviteTenantAsync(id, req, ct));

        // Inspections
        api.MapGet("/inspections", (Guid? propertyId, InspectionStatus? status, InspectionService s, CancellationToken ct) => s.ListAsync(propertyId, status, ct));
        api.MapPost("/inspections", (CreateInspectionRequest req, InspectionService s, CancellationToken ct) => s.CreateAsync(req, ct));
        api.MapGet("/inspections/{id:guid}", (Guid id, InspectionService s, CancellationToken ct) => s.GetAsync(id, ct));
        api.MapPut("/inspections/{id:guid}", (Guid id, UpdateDraftRequest req, InspectionService s, CancellationToken ct) => s.UpdateDraftAsync(id, req, ct));
        api.MapPost("/inspections/{id:guid}/publish", (Guid id, PublishRequest? req, InspectionService s, CancellationToken ct) => s.PublishAsync(id, req?.InviteEmail, ct));
        api.MapPost("/inspections/{id:guid}/invitation", (Guid id, RegenerateInvitationRequest? req, InspectionService s, CancellationToken ct) => s.RegenerateInvitationAsync(id, req?.InviteEmail, ct));
        api.MapPost("/inspections/{id:guid}/cancel", (Guid id, InspectionService s, CancellationToken ct) => s.CancelAsync(id, ct));
        api.MapPost("/inspections/{id:guid}/send-to-tenant", (Guid id, InspectionService s, CancellationToken ct) => s.SendToTenantAsync(id, ct));

        // Report sharing
        api.MapPost("/reports/{id:guid}/share-links", (Guid id, ShareLinkRequest? req, ReportService s, CancellationToken ct) => s.CreateShareLinkAsync(id, req?.Days, ct));
        api.MapDelete("/reports/{id:guid}/share-links/{linkId:guid}", async (Guid id, Guid linkId, ReportService s, CancellationToken ct) =>
        {
            await s.RevokeShareLinkAsync(id, linkId, ct);
            return Results.NoContent();
        });
    }
}

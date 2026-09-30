using InspectFlow.Api.Infrastructure;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InspectFlow.Api.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var agent = app.MapGroup("/api/agent").RequireAuthorization(Policies.Agent).WithTags("Agent");

        agent.MapGet("/dashboard", (AgentInspectionService s, CancellationToken ct) => s.DashboardAsync(ct));
        agent.MapGet("/available", (AgentInspectionService s, CancellationToken ct) => s.ListAvailableAsync(ct));
        agent.MapGet("/available/{id:guid}", (Guid id, AgentInspectionService s, CancellationToken ct) => s.GetAvailableAsync(id, ct));

        agent.MapGet("/invitations/{token}", (string token, InvitationService s, CancellationToken ct) => s.PreviewAsync(token, ct))
            .RequireRateLimiting(RateLimits.Invitation);
        agent.MapPost("/invitations/{token}/verify", (string token, VerifyInvitationRequest req, InvitationService s, CancellationToken ct) =>
            s.VerifyAsync(token, req.AccessCode, ct)).RequireRateLimiting(RateLimits.Invitation);

        var i = agent.MapGroup("/inspections");
        i.MapGet("", (string? scope, AgentInspectionService s, CancellationToken ct) => s.ListMineAsync(scope == "completed", ct));
        i.MapGet("/{id:guid}", (Guid id, AgentInspectionService s, CancellationToken ct) => s.GetAsync(id, ct));
        i.MapPost("/{id:guid}/accept", (Guid id, AgentInspectionService s, CancellationToken ct) => s.AcceptAsync(id, ct));
        i.MapPost("/{id:guid}/start", (Guid id, AgentInspectionService s, CancellationToken ct) => s.StartAsync(id, ct));
        i.MapPost("/{id:guid}/submit-review", (Guid id, AgentInspectionService s, CancellationToken ct) => s.SubmitForReviewAsync(id, ct));
        i.MapPost("/{id:guid}/return-to-progress", (Guid id, AgentInspectionService s, CancellationToken ct) => s.ReturnToInProgressAsync(id, ct));
        i.MapGet("/{id:guid}/review", (Guid id, ReportService s, CancellationToken ct) => s.GetReviewAsync(id, ct));
        i.MapPost("/{id:guid}/finalize", (Guid id, FinalizationService s, CancellationToken ct) => s.FinalizeAsync(id, ct));
        i.MapGet("/{id:guid}/analyses/{analysisId:guid}", (Guid id, Guid analysisId, AiAnalysisService s, CancellationToken ct) => s.GetAsync(id, analysisId, ct));
        i.MapDelete("/{id:guid}/photos/{mediaId:guid}", async (Guid id, Guid mediaId, MediaService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, mediaId, ct);
            return Results.NoContent();
        });

        var room = i.MapGroup("/{id:guid}/rooms/{roomId:guid}");
        room.MapGet("", (Guid id, Guid roomId, AgentInspectionService s, CancellationToken ct) => s.GetRoomAsync(id, roomId, ct));
        room.MapPut("", (Guid id, Guid roomId, UpdateRoomRequest req, AgentInspectionService s, CancellationToken ct) => s.UpdateRoomAsync(id, roomId, req, ct));
        room.MapPost("/complete", (Guid id, Guid roomId, AgentInspectionService s, CancellationToken ct) => s.CompleteRoomAsync(id, roomId, ct));
        room.MapPost("/reopen", (Guid id, Guid roomId, AgentInspectionService s, CancellationToken ct) => s.ReopenRoomAsync(id, roomId, ct));
        room.MapPost("/analysis", (Guid id, Guid roomId, AiAnalysisService s, CancellationToken ct) => s.RequestRoomAnalysisAsync(id, roomId, ct))
            .RequireRateLimiting(RateLimits.Ai);

        room.MapPost("/photos", async (Guid id, Guid roomId, [FromForm] IFormFile file, [FromForm] string? mediaType, [FromForm] Guid? defectId,
                MediaService s, IOptions<InspectionRulesOptions> rules, CancellationToken ct) =>
            {
                var type = string.Equals(mediaType, nameof(MediaType.Defect), StringComparison.OrdinalIgnoreCase) ? MediaType.Defect : MediaType.General;
                if (file.Length > rules.Value.MaxUploadBytes) throw new ValidationException("File", new($"The file is too large ({file.Length} bytes; maximum {rules.Value.MaxUploadBytes}).",
                    $"O arquivo é grande demais ({file.Length} bytes; máximo {rules.Value.MaxUploadBytes})."));
                await using var stream = file.OpenReadStream();
                return await s.UploadAsync(new UploadMediaCommand(id, roomId, type, defectId, stream, file.FileName, file.ContentType, file.Length), ct);
            })
            .DisableAntiforgery() // JWT bearer auth (no ambient cookies) — CSRF does not apply.
            .WithMetadata(new RequestSizeLimitAttribute(20 * 1024 * 1024))
            .RequireRateLimiting(RateLimits.Upload);

        room.MapPost("/defects", (Guid id, Guid roomId, AddDefectRequest req, AgentInspectionService s, CancellationToken ct) => s.AddDefectAsync(id, roomId, req, ct));
        room.MapPut("/defects/{defectId:guid}", (Guid id, Guid roomId, Guid defectId, UpdateDefectRequest req, AgentInspectionService s, CancellationToken ct) =>
            s.UpdateDefectAsync(id, roomId, defectId, req, ct));
        room.MapDelete("/defects/{defectId:guid}", (Guid id, Guid roomId, Guid defectId, AgentInspectionService s, CancellationToken ct) =>
            s.RemoveDefectAsync(id, roomId, defectId, ct));
        room.MapPost("/defects/{defectId:guid}/analysis", (Guid id, Guid roomId, Guid defectId, AiAnalysisService s, CancellationToken ct) =>
            s.RequestDefectAnalysisAsync(id, roomId, defectId, ct)).RequireRateLimiting(RateLimits.Ai);

        room.MapPost("/comparison/basic", (Guid id, Guid roomId, AgentInspectionService s, CancellationToken ct) => s.RunBasicComparisonAsync(id, roomId, ct));
        room.MapPost("/comparison/analysis", (Guid id, Guid roomId, AiAnalysisService s, CancellationToken ct) => s.RequestComparisonAnalysisAsync(id, roomId, ct))
            .RequireRateLimiting(RateLimits.Ai);
        room.MapPut("/comparison/decision", (Guid id, Guid roomId, ComparisonDecisionRequest req, AgentInspectionService s, CancellationToken ct) =>
            s.RecordComparisonDecisionAsync(id, roomId, req, ct));
    }
}

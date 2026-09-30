using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Modules.Tenants.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Tenants.Application;

public sealed record TenantInspectionDto(Guid Id, string InspectionType, string Status, string PropertyAddress, string CompanyName,
    DateTimeOffset? CompletedAt, DateTimeOffset? SentToTenantAt, DateTimeOffset? RespondedAt, string? ReportNumber);

public sealed record TenantDashboardDto(
    IReadOnlyList<TenantInspectionDto> AwaitingReview,
    IReadOnlyList<TenantInspectionDto> Accepted,
    IReadOnlyList<TenantInspectionDto> Disputed);

public sealed record AddObservationRequest(Guid? RoomId, string Text);

public sealed record TenantDecisionRequest(string? Comment);

/// <summary>The tenant's deliberately narrow surface: see own reports, comment, accept or dispute.</summary>
public sealed class TenantService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionWriteScope writeScope,
    ReportService reports,
    INotificationService notifications,
    IAuditLogger audit,
    IClock clock,
    IOptions<AppUrlOptions> urls)
{
    public async Task<TenantDashboardDto> DashboardAsync(CancellationToken ct)
    {
        var tenantId = access.RequireTenant();
        var rows = await (from i in access.TenantVisibleInspections(tenantId).AsNoTracking()
                          join p in db.Properties.AsNoTracking() on i.PropertyId equals p.Id
                          join c in db.Companies.AsNoTracking() on i.CompanyId equals c.Id
                          join r in db.InspectionReports.AsNoTracking() on i.Id equals r.InspectionId into reportsJoin
                          from report in reportsJoin.DefaultIfEmpty()
                          orderby i.SentToTenantAt descending
                          select new TenantInspectionDto(i.Id, i.InspectionType.ToString(), i.Status.ToString(),
                              p.AddressLine1 + ", " + p.City + " " + p.Postcode, c.Name, i.CompletedAt, i.SentToTenantAt,
                              i.TenantRespondedAt, report != null ? report.ReportNumber : null)).ToListAsync(ct);
        return new TenantDashboardDto(
            rows.Where(r => r.Status == nameof(InspectionStatus.AwaitingTenant)).ToList(),
            rows.Where(r => r.Status == nameof(InspectionStatus.Accepted)).ToList(),
            rows.Where(r => r.Status == nameof(InspectionStatus.Disputed)).ToList());
    }

    public async Task<ReportViewDto> GetReportAsync(Guid inspectionId, CancellationToken ct)
    {
        await LoadVisibleAsync(inspectionId, tracking: false, ct);
        return await reports.GetForInspectionAsync(inspectionId, ct);
    }

    public async Task<ReportViewDto> AddObservationAsync(Guid inspectionId, AddObservationRequest request, CancellationToken ct)
    {
        await LoadVisibleAsync(inspectionId, tracking: false, ct); // authorization before taking the lock
        // Lock so an observation can't be added concurrently with the accept/dispute decision.
        await writeScope.RunAsync(inspectionId, () => AddObservationCoreAsync(inspectionId, request, ct), ct);
        return await reports.GetForInspectionAsync(inspectionId, ct);
    }

    private async Task<bool> AddObservationCoreAsync(Guid inspectionId, AddObservationRequest request, CancellationToken ct)
    {
        var tenantId = access.RequireTenant();
        var inspection = await LoadVisibleAsync(inspectionId, tracking: false, ct);
        if (inspection.Status != InspectionStatus.AwaitingTenant)
            throw new DomainRuleException("tenant.review_closed", new("Observations can only be added while the report awaits your review.", "Observações só podem ser adicionadas enquanto o laudo aguarda a sua revisão."));
        var text = request.Text?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 4000)
            throw new ValidationException("Text", new("Observation text is required (max 4000 characters).", "O texto da observação é obrigatório (máximo de 4000 caracteres)."));
        if (request.RoomId is not null && !await db.InspectionRooms.AnyAsync(r => r.Id == request.RoomId && r.InspectionId == inspectionId, ct))
            throw new ValidationException("RoomId", new($"Room {request.RoomId} does not belong to this inspection.", $"O cômodo {request.RoomId} não pertence a esta vistoria."));

        var versionId = await LatestVersionIdAsync(inspectionId, ct);
        var observation = new TenantObservation
        {
            Id = Guid.NewGuid(),
            InspectionId = inspectionId,
            ReportVersionId = versionId,
            InspectionRoomId = request.RoomId,
            AuthorUserId = tenantId,
            Text = text,
            CreatedAt = clock.UtcNow,
        };
        db.TenantObservations.Add(observation);
        audit.Record(AuditActions.TenantCommented, nameof(Inspection), inspectionId, new { observationId = observation.Id, roomId = request.RoomId });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<ReportViewDto> AcceptAsync(Guid inspectionId, TenantDecisionRequest request, CancellationToken ct) =>
        RespondAsync(inspectionId, TenantDecision.Accepted, request.Comment, ct);

    public Task<ReportViewDto> DisputeAsync(Guid inspectionId, TenantDecisionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException("Comment", new("Please explain what you disagree with.", "Explique com o que você não concorda."));
        return RespondAsync(inspectionId, TenantDecision.Disputed, request.Comment, ct);
    }

    /// <summary>AwaitingTenant → Accepted/Disputed. Optimistic concurrency ensures only one decision wins.</summary>
    private async Task<ReportViewDto> RespondAsync(Guid inspectionId, TenantDecision decision, string? comment, CancellationToken ct)
    {
        var tenantId = access.RequireTenant();
        var inspection = await LoadVisibleAsync(inspectionId, tracking: true, ct);
        var now = clock.UtcNow;
        inspection.RecordTenantDecision(decision == TenantDecision.Accepted, tenantId, now);

        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed is { Length: > 4000 }) throw new ValidationException("Comment", new("Comment must be at most 4000 characters.", "O comentário deve ter no máximo 4000 caracteres."));
        db.TenantResponses.Add(new TenantResponse
        {
            Id = Guid.NewGuid(),
            InspectionId = inspectionId,
            ReportVersionId = await LatestVersionIdAsync(inspectionId, ct),
            UserId = tenantId,
            Decision = decision,
            Comment = trimmed,
            CreatedAt = now,
        });
        audit.Record(decision == TenantDecision.Accepted ? AuditActions.TenantAccepted : AuditActions.TenantDisputed,
            nameof(Inspection), inspectionId, new { hasComment = trimmed is not null });

        var companyUsers = await db.CompanyMembers.AsNoTracking().Where(m => m.CompanyId == inspection.CompanyId).Select(m => m.UserId).ToListAsync(ct);
        foreach (var userId in companyUsers)
            notifications.Enqueue(new NotificationMessage(NotificationTypes.TenantResponded,
                decision == TenantDecision.Accepted ? "Tenant accepted the inspection report" : "Tenant disputed the inspection report",
                trimmed ?? "No comment provided.", RecipientUserId: userId, Link: urls.Value.Web($"/company/inspections/{inspectionId}")));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(new("This report was already answered. Refresh to see the latest status.", "Este laudo já foi respondido. Atualize a página para ver o status mais recente."));
        }
        return await reports.GetForInspectionAsync(inspectionId, ct);
    }

    private async Task<Inspection> LoadVisibleAsync(Guid inspectionId, bool tracking, CancellationToken ct)
    {
        var tenantId = access.RequireTenant();
        var query = access.TenantVisibleInspections(tenantId);
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(i => i.Id == inspectionId, ct) ?? throw new NotFoundException(EntityNames.Inspection, inspectionId);
    }

    private Task<Guid> LatestVersionIdAsync(Guid inspectionId, CancellationToken ct) =>
        (from r in db.InspectionReports
         join v in db.InspectionReportVersions on r.Id equals v.ReportId
         where r.InspectionId == inspectionId && v.VersionNumber == r.LatestVersionNumber
         select v.Id).FirstAsync(ct);
}

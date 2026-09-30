using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Reports.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Reports.Application;

public sealed record TenantObservationDto(Guid Id, Guid? RoomId, string? RoomName, string AuthorName, string Text, DateTimeOffset CreatedAt);

public sealed record TenantResponseDto(Guid Id, string UserName, string Decision, string? Comment, DateTimeOffset CreatedAt);

public sealed record ReportVersionDto(int VersionNumber, DateTimeOffset GeneratedAt, string Reason, string SnapshotSha256, string? PdfSha256);

public sealed record ReportViewDto(
    Guid ReportId,
    string ReportNumber,
    int VersionNumber,
    DateTimeOffset GeneratedAt,
    string SnapshotSha256,
    Guid InspectionId,
    string InspectionStatus,
    string ViewerKind,
    ReportSnapshot Snapshot,
    IReadOnlyDictionary<Guid, string> PhotoUrls,
    string? PdfUrl,
    IReadOnlyList<ReportVersionDto> Versions,
    IReadOnlyList<TenantObservationDto> Observations,
    IReadOnlyList<TenantResponseDto> Responses,
    bool CanRespond);

public sealed record ShareLinkDto(Guid Id, string Link, DateTimeOffset ExpiresAt);

public sealed class ReportService(
    IAppDbContext db,
    InspectionAccess access,
    ICurrentUser currentUser,
    IStorageService storage,
    MediaUrlService mediaUrls,
    ReportSnapshotBuilder builder,
    RoomContextLoader contexts,
    InspectionDetailsBuilder details,
    IAuditLogger audit,
    IClock clock,
    IOptions<AppUrlOptions> urls,
    IOptions<InspectionRulesOptions> rules)
{
    public async Task<ReportViewDto> GetForInspectionAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await db.Inspections.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inspectionId, ct)
                         ?? throw new NotFoundException("Report");
        var viewer = await access.RequireReportViewerAsync(inspection, ct);
        var report = await db.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.InspectionId == inspectionId, ct)
                     ?? throw new NotFoundException("Report");
        if (viewer == InspectionViewerKind.Tenant)
            audit.Record(AuditActions.TenantViewed, nameof(InspectionReport), report.Id, new { inspectionId });
        await db.SaveChangesAsync(ct);
        return await BuildViewAsync(report, inspection, viewer.ToString(), redactForPublic: false, ct);
    }

    public async Task<ReportViewDto> GetAsync(Guid reportId, CancellationToken ct)
    {
        var report = await db.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, ct)
                     ?? throw new NotFoundException("Report");
        return await GetForInspectionAsync(report.InspectionId, ct);
    }

    /// <summary>Anonymous access through a share link (token stored as a hash, expiring, revocable).</summary>
    public async Task<ReportViewDto> GetSharedAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) throw new NotFoundException("Report");
        var hash = SecureTokens.Sha256Hex(token);
        var now = clock.UtcNow;
        var link = await db.ReportShareLinks.FirstOrDefaultAsync(l => l.TokenHash == hash, ct);
        if (link is null || !link.IsActive(now)) throw new NotFoundException("Report");
        link.LastAccessedAt = now;
        await db.SaveChangesAsync(ct);
        var report = await db.InspectionReports.AsNoTracking().FirstAsync(r => r.Id == link.ReportId, ct);
        var inspection = await db.Inspections.AsNoTracking().FirstAsync(i => i.Id == report.InspectionId, ct);
        return await BuildViewAsync(report, inspection, "Shared", redactForPublic: true, ct);
    }

    public async Task<ShareLinkDto> CreateShareLinkAsync(Guid reportId, int? days, CancellationToken ct)
    {
        var report = await db.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, ct)
                     ?? throw new NotFoundException("Report");
        await access.GetForCompanyAsync(report.InspectionId, CompanyPermission.ManageInspections, ct, tracking: false);
        var now = clock.UtcNow;
        var token = SecureTokens.Create();
        var lifetime = Math.Clamp(days ?? rules.Value.ReportShareLinkDays, 1, 365);
        var link = new ReportShareLink
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            TokenHash = SecureTokens.Sha256Hex(token),
            CreatedAt = now,
            CreatedBy = currentUser.RequireUserId(),
            ExpiresAt = now.AddDays(lifetime),
        };
        db.ReportShareLinks.Add(link);
        audit.Record(AuditActions.ReportShared, nameof(InspectionReport), report.Id, new { linkId = link.Id, expiresAt = link.ExpiresAt });
        await db.SaveChangesAsync(ct);
        return new ShareLinkDto(link.Id, urls.Value.Web($"/reports/shared/{token}"), link.ExpiresAt);
    }

    public async Task RevokeShareLinkAsync(Guid reportId, Guid linkId, CancellationToken ct)
    {
        var report = await db.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, ct)
                     ?? throw new NotFoundException("Report");
        await access.GetForCompanyAsync(report.InspectionId, CompanyPermission.ManageInspections, ct, tracking: false);
        var link = await db.ReportShareLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.ReportId == reportId, ct)
                   ?? throw new NotFoundException("Share link", linkId);
        link.RevokedAt ??= clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Live preview for the agent's review page, built with the same builder used at finalization.</summary>
    public async Task<ReviewDto> GetReviewAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct, tracking: false);
        var preview = await builder.BuildAsync(inspection, Guid.Empty, "PREVIEW", 0, clock.UtcNow, ct);
        preview = preview with { Tenants = preview.Tenants.Select(t => t with { Email = null }).ToList() }; // agents don't need tenant emails
        var issues = InspectionReadiness.GetBlockingIssues(inspection, await contexts.LoadAsync(inspection, ct), requireRoomsCompleted: true);
        return new ReviewDto(await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct), preview,
            await PhotoUrlsAsync(preview, ct), issues, inspection.Status == InspectionStatus.Review && issues.Count == 0);
    }

    private async Task<ReportViewDto> BuildViewAsync(InspectionReport report, Inspection inspection, string viewerKind,
        bool redactForPublic, CancellationToken ct)
    {
        var versions = await db.InspectionReportVersions.AsNoTracking().Where(v => v.ReportId == report.Id)
            .OrderByDescending(v => v.VersionNumber).ToListAsync(ct);
        var latest = versions.First();
        var snapshot = ReportSnapshotStore.Deserialize(latest.SnapshotJson);
        if (viewerKind == nameof(InspectionViewerKind.Agent))
            snapshot = snapshot with { Tenants = snapshot.Tenants.Select(t => t with { Email = null }).ToList() };
        if (redactForPublic)
        {
            snapshot = snapshot with
            {
                Agent = snapshot.Agent is null ? null : snapshot.Agent with { Email = null, UserId = null },
                Tenants = snapshot.Tenants.Select(t => t with { Email = null, UserId = null }).ToList(),
            };
        }

        var observations = await (from o in db.TenantObservations.AsNoTracking()
                                  join u in db.Users.AsNoTracking() on o.AuthorUserId equals u.Id
                                  where o.InspectionId == inspection.Id
                                  orderby o.CreatedAt
                                  select new { o, u.FullName }).ToListAsync(ct);
        var responses = await (from r in db.TenantResponses.AsNoTracking()
                               join u in db.Users.AsNoTracking() on r.UserId equals u.Id
                               where r.InspectionId == inspection.Id
                               orderby r.CreatedAt
                               select new TenantResponseDto(r.Id, u.FullName, r.Decision.ToString(), r.Comment, r.CreatedAt)).ToListAsync(ct);

        var pdfUrl = latest.PdfStorageKey is null ? null
            : await storage.GetReadUrlAsync(latest.PdfStorageKey, TimeSpan.FromMinutes(rules.Value.MediaUrlMinutes), ct);

        return new ReportViewDto(report.Id, report.ReportNumber, latest.VersionNumber, latest.GeneratedAt, latest.SnapshotSha256,
            inspection.Id, inspection.Status.ToString(), viewerKind, snapshot, await PhotoUrlsAsync(snapshot, ct), pdfUrl,
            versions.Select(v => new ReportVersionDto(v.VersionNumber, v.GeneratedAt, v.Reason, v.SnapshotSha256, v.PdfSha256)).ToList(),
            observations.Select(x => new TenantObservationDto(x.o.Id, x.o.InspectionRoomId,
                snapshot.Rooms.FirstOrDefault(r => r.Id == x.o.InspectionRoomId)?.Name, x.FullName, x.o.Text, x.o.CreatedAt)).ToList(),
            responses,
            viewerKind == nameof(InspectionViewerKind.Tenant) && inspection.Status == InspectionStatus.AwaitingTenant);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> PhotoUrlsAsync(ReportSnapshot snapshot, CancellationToken ct)
    {
        var photos = snapshot.Rooms.SelectMany(r => r.Photos
                .Concat(r.Defects.SelectMany(d => d.Photos))
                .Concat(r.Comparison?.BaselinePhotos ?? []))
            .DistinctBy(p => p.MediaId);
        var result = new Dictionary<Guid, string>();
        foreach (var p in photos) result[p.MediaId] = await mediaUrls.GetUrlAsync(p.StorageKey, ct);
        return result;
    }
}

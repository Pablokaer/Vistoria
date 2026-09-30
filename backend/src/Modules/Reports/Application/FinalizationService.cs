using System.Security.Cryptography;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Reports.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Reports.Application;

public sealed record FinalizeResultDto(Guid InspectionId, string Status, Guid ReportId, string ReportNumber, int Version, string SnapshotSha256);

/// <summary>
/// Review → Completed (→ AwaitingTenant). Validates readiness, freezes the report into an immutable
/// version (JSON snapshot + PDF + hashes) and hands it to the tenants — all in one transaction holding
/// an exclusive lock on the inspection row.
/// </summary>
public sealed class FinalizationService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionWriteScope writeScope,
    RoomContextLoader contexts,
    ReportSnapshotBuilder builder,
    ReportImageLoader images,
    IPdfService pdf,
    IStorageService storage,
    INotificationService notifications,
    IAuditLogger audit,
    IClock clock,
    IOptions<AppUrlOptions> urls,
    ILogger<FinalizationService> logger)
{
    public async Task<FinalizeResultDto> FinalizeAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        string? pdfKey = null;
        try
        {
            return await writeScope.RunAsync(inspectionId, async () =>
            {
                var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
                var issues = InspectionReadiness.GetBlockingIssues(inspection, await contexts.LoadAsync(inspection, ct), requireRoomsCompleted: true);
                var now = clock.UtcNow;
                inspection.Finalize(agentId, issues, now);

                if (await db.InspectionReports.AnyAsync(r => r.InspectionId == inspection.Id, ct))
                    throw new DomainRuleException("report.exists", "A report already exists for this inspection.");

                var report = new InspectionReport
                {
                    Id = Guid.NewGuid(),
                    InspectionId = inspection.Id,
                    ReportNumber = NewReportNumber(now),
                    CreatedAt = now,
                };
                var snapshot = await builder.BuildAsync(inspection, report.Id, report.ReportNumber, 1, now, ct);
                var json = ReportSnapshotStore.Serialize(snapshot);
                var version = report.AddVersion(json, SecureTokens.Sha256Hex(json), agentId, "Initial finalization", now);

                var pdfBytes = pdf.RenderInspectionReport(snapshot, await images.LoadAsync(snapshot, ct));
                pdfKey = StorageKeys.ForReportPdf(report.Id, version.VersionNumber);
                using (var stream = new MemoryStream(pdfBytes))
                    await storage.SaveAsync(pdfKey, stream, "application/pdf", ct);
                version.PdfStorageKey = pdfKey;
                version.PdfSha256 = SecureTokens.Sha256Hex(pdfBytes);
                version.PdfSizeBytes = pdfBytes.LongLength;

                db.InspectionReports.Add(report);
                audit.Record(AuditActions.InspectionFinalized, nameof(Inspection), inspection.Id, new { reportId = report.Id });
                audit.Record(AuditActions.ReportGenerated, nameof(InspectionReport), report.Id,
                    new { version = version.VersionNumber, snapshotSha256 = version.SnapshotSha256, pdfSha256 = version.PdfSha256, pdfBytes = version.PdfSizeBytes });

                var companyUsers = await db.CompanyMembers.AsNoTracking().Where(m => m.CompanyId == inspection.CompanyId).Select(m => m.UserId).ToListAsync(ct);
                foreach (var userId in companyUsers)
                    notifications.Enqueue(new NotificationMessage(NotificationTypes.InspectionFinalized, "Inspection report finalized",
                        $"Report {report.ReportNumber} is available.", RecipientUserId: userId,
                        Link: urls.Value.Web($"/company/inspections/{inspection.Id}")));

                var members = inspection.TenancyId is null ? []
                    : await db.TenancyMembers.AsNoTracking().Where(m => m.TenancyId == inspection.TenancyId).ToListAsync(ct);
                if (members.Count > 0)
                {
                    inspection.SendToTenant(now);
                    TenantNotifications.NotifyReportReady(notifications, urls.Value, inspection, members);
                    audit.Record(AuditActions.InspectionSentToTenant, nameof(Inspection), inspection.Id, new { tenants = members.Count });
                }

                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new ConflictException("The inspection was changed while finalizing. Refresh and try again.");
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new ConflictException("This inspection has already been finalized.");
                }
                return new FinalizeResultDto(inspection.Id, inspection.Status.ToString(), report.Id, report.ReportNumber,
                    version.VersionNumber, version.SnapshotSha256);
            }, ct, exclusive: true);
        }
        catch when (pdfKey is not null)
        {
            // Transaction rolled back: remove the orphaned PDF (best effort).
            try { await storage.DeleteAsync(pdfKey, CancellationToken.None); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete orphaned PDF {Key}", pdfKey); }
            throw;
        }
    }

    /// <summary>Non-sequential, human-friendly id, e.g. IR-202609-7KQ4M2XZ.</summary>
    public static string NewReportNumber(DateTimeOffset now)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return $"IR-{now:yyyyMM}-{new string(chars)}";
    }
}

/// <summary>Loads the photo bytes referenced by a snapshot (for PDF rendering).</summary>
public sealed class ReportImageLoader(IStorageService storage, ILogger<ReportImageLoader> logger)
{
    public async Task<IReadOnlyDictionary<string, byte[]>> LoadAsync(ReportSnapshot snapshot, CancellationToken ct)
    {
        var keys = snapshot.Rooms.SelectMany(r => r.Photos.Concat(r.Defects.SelectMany(d => d.Photos)))
            .Select(p => p.StorageKey).Distinct().ToList();
        var result = new Dictionary<string, byte[]>(keys.Count);
        foreach (var key in keys)
        {
            try
            {
                await using var stream = await storage.OpenReadAsync(key, ct);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                result[key] = ms.ToArray();
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException)
            {
                logger.LogWarning(ex, "Photo {Key} missing while rendering report", key);
            }
        }
        return result;
    }
}

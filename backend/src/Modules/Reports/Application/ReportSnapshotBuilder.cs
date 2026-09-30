using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Reports.Application;

/// <summary>Builds a <see cref="ReportSnapshot"/> from the current inspection data (used for review preview and finalization).</summary>
public sealed class ReportSnapshotBuilder(IAppDbContext db, ReportSnapshotStore snapshots)
{
    public async Task<ReportSnapshot> BuildAsync(Inspection inspection, Guid reportId, string reportNumber, int version,
        DateTimeOffset generatedAt, CancellationToken ct)
    {
        var company = await db.Companies.AsNoTracking().FirstAsync(c => c.Id == inspection.CompanyId, ct);
        var property = await db.Properties.AsNoTracking().FirstAsync(p => p.Id == inspection.PropertyId, ct);
        var tenancy = inspection.TenancyId is null ? null
            : await db.Tenancies.AsNoTracking().Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == inspection.TenancyId, ct);

        ReportPerson? agent = null;
        if (inspection.AgentId is not null)
        {
            agent = await db.Users.AsNoTracking().Where(u => u.Id == inspection.AgentId)
                .Select(u => new ReportPerson(u.Id, u.FullName, u.Email)).FirstOrDefaultAsync(ct);
        }

        var media = await db.InspectionRoomMedia.AsNoTracking()
            .Where(m => m.InspectionId == inspection.Id && m.Status == MediaStatus.Ready)
            .OrderBy(m => m.UploadedAt).ToListAsync(ct);
        var comparisons = await db.InspectionComparisons.AsNoTracking()
            .Where(c => c.TargetInspectionId == inspection.Id).ToListAsync(ct);

        ReportSnapshot? baseline = null;
        string? baselineReportNumber = null;
        DateTimeOffset? baselineCompletedAt = null;
        if (inspection.ComparisonInspectionId is not null)
        {
            baseline = await snapshots.GetLatestForInspectionAsync(inspection.ComparisonInspectionId.Value, ct);
            baselineReportNumber = baseline?.ReportNumber;
            baselineCompletedAt = baseline?.Inspection.CompletedAt;
        }

        static ReportPhoto Photo(InspectionRoomMedia m) => new(m.Id, m.StorageKey, m.MimeType, m.Caption, m.UploadedAt, m.Sha256);

        var rooms = new List<ReportRoom>();
        foreach (var room in inspection.Rooms.OrderBy(r => r.Sequence))
        {
            var defects = room.Defects.OrderBy(d => d.CreatedAt).Select(d => new ReportDefect(
                d.Id, d.Description, d.Location, d.Classification.ToString(), d.FinalDescription, d.AIDescription, d.AIConfidence,
                media.Where(m => m.DefectId == d.Id).Select(Photo).ToList())).ToList();

            ReportRoomComparison? roomComparison = null;
            var comparison = comparisons.FirstOrDefault(c => c.InspectionRoomId == room.Id);
            if (comparison is not null)
            {
                var baselineRoom = baseline?.Rooms.FirstOrDefault(r => r.Id == comparison.SourceInspectionRoomId);
                roomComparison = new ReportRoomComparison(
                    comparison.SourceInspectionRoomId,
                    baselineRoom?.Description,
                    baselineRoom?.Defects.Select(d => d.Description ?? d.Title ?? "Defect").ToList() ?? [],
                    baselineRoom?.Photos ?? [],
                    comparison.BasicComparison,
                    comparison.AIAnalysis,
                    comparison.AgentDecision?.ToString(),
                    comparison.AgentNotes);
            }

            rooms.Add(new ReportRoom(room.Id, room.OriginalPropertyRoomId, room.Name, room.RoomType.ToString(), room.Sequence,
                room.FinalDescription, room.AiDescription, room.LatestAiAnalysisId, room.FinalDescriptionSource.ToString(), room.AgentNotes,
                room.DefectsFound, media.Where(m => m.InspectionRoomId == room.Id && m.MediaType == MediaType.General).Select(Photo).ToList(),
                defects, roomComparison));
        }

        ReportComparisonSummary? summary = null;
        if (inspection.ComparisonInspectionId is not null && comparisons.Count > 0)
        {
            var items = rooms.Where(r => r.Comparison is not null)
                .Select(r => new ComparisonSummaryItem(r.Name, r.Comparison!.Decision ?? "Pending", r.Comparison.Notes)).ToList();
            summary = new ReportComparisonSummary(inspection.ComparisonInspectionId.Value, baselineReportNumber, items.Count,
                items.GroupBy(i => i.Decision).ToDictionary(g => g.Key, g => g.Count()), items);
        }

        return new ReportSnapshot(
            ReportSnapshot.CurrentSchemaVersion,
            reportId,
            reportNumber,
            version,
            generatedAt,
            new ReportCompany(company.Id, company.Name, company.ContactEmail, company.Phone),
            new ReportProperty(property.Id, property.AddressLine1, property.AddressLine2, property.City, property.Postcode,
                property.Country, property.PropertyType.ToString()),
            new ReportInspection(inspection.Id, inspection.InspectionType.ToString(), inspection.ScheduledDate, inspection.StartedAt,
                inspection.CompletedAt ?? generatedAt, inspection.TenancyId, tenancy?.Reference, tenancy?.StartDate, tenancy?.EndDate,
                inspection.ComparisonInspectionId, baselineReportNumber, baselineCompletedAt),
            agent,
            tenancy?.Members.OrderBy(m => m.InvitedAt).Select(m => new ReportPerson(m.UserId, m.FullName, m.Email)).ToList() ?? [],
            rooms,
            summary,
            company.ReportLanguage);
    }
}

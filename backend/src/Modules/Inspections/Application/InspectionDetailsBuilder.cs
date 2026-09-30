using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Tenancies.Application;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Inspections.Application;

public sealed class InspectionDetailsBuilder(IAppDbContext db, IClock clock)
{
    public async Task<InspectionDetailsDto> BuildAsync(Inspection inspection, InspectionViewerKind viewer, CancellationToken ct)
    {
        var property = await db.Properties.AsNoTracking().FirstAsync(p => p.Id == inspection.PropertyId, ct);
        var companyName = await db.Companies.AsNoTracking().Where(c => c.Id == inspection.CompanyId).Select(c => c.Name).FirstAsync(ct);

        TenancyDto? tenancy = null;
        if (inspection.TenancyId is not null)
        {
            var t = await db.Tenancies.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == inspection.TenancyId, ct);
            if (t is not null)
            {
                tenancy = TenancyService.ToDto(t);
                // Agents only need tenant names; emails stay with the company.
                if (viewer != InspectionViewerKind.Company)
                    tenancy = tenancy with { Members = tenancy.Members.Select(m => m with { Email = string.Empty }).ToList() };
            }
        }

        PersonDto? agent = null;
        if (inspection.AgentId is not null)
        {
            agent = await (from u in db.Users.AsNoTracking()
                           join p in db.AgentProfiles.AsNoTracking() on u.Id equals p.UserId into profiles
                           from profile in profiles.DefaultIfEmpty()
                           where u.Id == inspection.AgentId
                           select new PersonDto(u.Id, u.FullName, u.Email, profile != null ? profile.Phone : null)).FirstOrDefaultAsync(ct);
        }

        ComparisonRefDto? comparison = null;
        if (inspection.ComparisonInspectionId is not null)
        {
            comparison = await (from i in db.Inspections.AsNoTracking()
                                join r in db.InspectionReports.AsNoTracking() on i.Id equals r.InspectionId into sourceReports
                                from sourceReport in sourceReports.DefaultIfEmpty()
                                where i.Id == inspection.ComparisonInspectionId
                                select new ComparisonRefDto(i.Id, i.InspectionType.ToString(), i.CompletedAt, sourceReport != null ? sourceReport.ReportNumber : null))
                .FirstOrDefaultAsync(ct);
        }

        var media = await db.InspectionRoomMedia.AsNoTracking()
            .Where(m => m.InspectionId == inspection.Id && m.MediaType == MediaType.General)
            .GroupBy(m => m.InspectionRoomId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var decisions = await db.InspectionComparisons.AsNoTracking().Where(c => c.TargetInspectionId == inspection.Id)
            .Select(c => new { c.InspectionRoomId, Decided = c.AgentDecision != null }).ToListAsync(ct);

        var rooms = inspection.Rooms.OrderBy(r => r.Sequence).Select(r => new RoomProgressDto(
            r.Id, r.Name, r.RoomType.ToString(), r.Sequence, r.Status.ToString(),
            media.FirstOrDefault(m => m.Key == r.Id)?.Count ?? 0,
            r.Defects.Count,
            !string.IsNullOrWhiteSpace(r.AiDescription),
            !string.IsNullOrWhiteSpace(r.FinalDescription),
            decisions.FirstOrDefault(d => d.InspectionRoomId == r.Id)?.Decided)).ToList();

        var report = await db.InspectionReports.AsNoTracking().Where(r => r.InspectionId == inspection.Id)
            .Select(r => new ReportRefDto(r.Id, r.ReportNumber, r.LatestVersionNumber,
                r.Versions.Where(v => v.VersionNumber == r.LatestVersionNumber).Select(v => v.GeneratedAt).First()))
            .FirstOrDefaultAsync(ct);

        InvitationStatusDto? invitation = null;
        if (viewer == InspectionViewerKind.Company && inspection.Visibility == InspectionVisibility.Private)
        {
            var now = clock.UtcNow;
            invitation = await db.InspectionInvitations.AsNoTracking().Where(x => x.InspectionId == inspection.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new InvitationStatusDto(x.CreatedAt, x.ExpiresAt, x.AttemptCount, x.MaxAttempts, x.UsedAt != null,
                    x.UsedAt, x.RevokedAt != null, x.ExpiresAt <= now, x.InvitedEmail))
                .FirstOrDefaultAsync(ct);
        }

        var hideAddress = viewer == InspectionViewerKind.Agent && inspection.AgentId is null;
        var propertyRef = new PropertyRefDto(property.Id, hideAddress ? string.Empty : property.AddressLine1,
            hideAddress ? null : property.AddressLine2, property.City, hideAddress ? PostcodeArea(property.Postcode) : property.Postcode,
            property.Country, property.PropertyType.ToString());

        return new InspectionDetailsDto(
            inspection.Id, inspection.InspectionType.ToString(), inspection.Visibility.ToString(), inspection.Status.ToString(),
            propertyRef, tenancy, agent, companyName, comparison, inspection.Instructions, inspection.ScheduledDate, inspection.AcceptBy,
            inspection.CreatedAt, inspection.PublishedAt, inspection.AcceptedAt, inspection.StartedAt, inspection.CompletedAt,
            inspection.SentToTenantAt, inspection.TenantRespondedAt,
            rooms, rooms.Count(r => r.Status == nameof(InspectionRoomStatus.Completed)), report, invitation,
            AllowedActions(inspection, viewer, tenancy, rooms));
    }

    public static string PostcodeArea(string postcode)
    {
        var trimmed = postcode.Trim();
        var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        if (space > 0) return trimmed[..space];
        return trimmed.Length > 3 ? trimmed[..3] : trimmed;
    }

    private static List<string> AllowedActions(Inspection i, InspectionViewerKind viewer, TenancyDto? tenancy, IReadOnlyList<RoomProgressDto> rooms)
    {
        var actions = new List<string>();
        if (viewer == InspectionViewerKind.Company)
        {
            if (i.Status == InspectionStatus.Draft) actions.AddRange(["publish", "edit"]);
            if (InspectionStateMachine.CanTransition(i.Status, InspectionStatus.Cancelled)) actions.Add("cancel");
            if (i.Status == InspectionStatus.Open && i.Visibility == InspectionVisibility.Private) actions.Add("regenerateInvitation");
            if (i.Status == InspectionStatus.Completed && tenancy is { Members.Count: > 0 }) actions.Add("sendToTenant");
            if (i.IsFinalized) actions.AddRange(["viewReport", "shareReport"]);
        }
        else if (viewer == InspectionViewerKind.Agent)
        {
            if (i.Status == InspectionStatus.Open && i.AgentId is null) actions.Add("accept");
            if (i.AgentId is null) return actions;
            if (i.Status == InspectionStatus.Assigned) actions.Add("start");
            if (i.Status == InspectionStatus.InProgress && rooms.All(r => r.Status == nameof(InspectionRoomStatus.Completed)))
                actions.Add("submitForReview");
            if (i.Status == InspectionStatus.Review) actions.AddRange(["finalize", "returnToInProgress"]);
            if (InspectionStateMachine.IsEditable(i.Status)) actions.Add("edit");
            if (i.IsFinalized) actions.Add("viewReport");
        }
        return actions;
    }
}

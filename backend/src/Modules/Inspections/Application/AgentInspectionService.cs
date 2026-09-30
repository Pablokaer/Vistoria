using System.Text.Json;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Agent-side use cases: marketplace, accept (race-safe), start, room execution, defects, comparison, review.</summary>
public sealed class AgentInspectionService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionService inspections,
    InspectionDetailsBuilder details,
    RoomContextLoader contexts,
    InspectionWriteScope writeScope,
    MediaUrlService mediaUrls,
    ReportSnapshotStore snapshots,
    IImageAnalysisService analyzer,
    INotificationService notifications,
    IAuditLogger audit,
    IClock clock,
    IOptions<AppUrlOptions> urls)
{
    // ---------- Marketplace ----------

    public async Task<IReadOnlyList<AvailableInspectionDto>> ListAvailableAsync(CancellationToken ct)
    {
        access.RequireAgent();
        var now = clock.UtcNow;
        var rows = await (from i in db.Inspections.AsNoTracking()
                          join p in db.Properties.AsNoTracking() on i.PropertyId equals p.Id
                          join c in db.Companies.AsNoTracking() on i.CompanyId equals c.Id
                          where i.Status == InspectionStatus.Open && i.Visibility == InspectionVisibility.Public &&
                                i.AgentId == null && (i.AcceptBy == null || i.AcceptBy > now)
                          orderby i.PublishedAt descending
                          select new { i, p.City, p.Postcode, p.PropertyType, CompanyName = c.Name, Rooms = i.Rooms.OrderBy(r => r.Sequence).Select(r => r.Name).ToList() })
            .Take(200).ToListAsync(ct);
        return rows.Select(r => ToAvailable(r.i, r.CompanyName, r.City, r.Postcode, r.PropertyType.ToString(), r.Rooms)).ToList();
    }

    public async Task<AvailableInspectionDto> GetAvailableAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var row = await (from i in db.Inspections.AsNoTracking()
                         join p in db.Properties.AsNoTracking() on i.PropertyId equals p.Id
                         join c in db.Companies.AsNoTracking() on i.CompanyId equals c.Id
                         where i.Id == inspectionId && i.Status == InspectionStatus.Open
                         select new { i, p.City, p.Postcode, p.PropertyType, CompanyName = c.Name, Rooms = i.Rooms.OrderBy(r => r.Sequence).Select(r => r.Name).ToList() })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Inspection", inspectionId);
        if (row.i.Visibility == InspectionVisibility.Private && !await HasVerifiedInvitationAsync(row.i.Id, agentId, ct))
            throw new NotFoundException("Inspection", inspectionId);
        return ToAvailable(row.i, row.CompanyName, row.City, row.Postcode, row.PropertyType.ToString(), row.Rooms);
    }

    /// <summary>
    /// Open → Assigned. Protected by optimistic concurrency (xmin): if two agents accept at the same time,
    /// exactly one succeeds and the other gets a conflict.
    /// </summary>
    public async Task<InspectionDetailsDto> AcceptAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await db.Inspections.Include(i => i.Rooms).ThenInclude(r => r.Defects)
                             .FirstOrDefaultAsync(i => i.Id == inspectionId, ct)
                         ?? throw new NotFoundException("Inspection", inspectionId);

        if (inspection.Visibility == InspectionVisibility.Private)
        {
            if (!await HasVerifiedInvitationAsync(inspection.Id, agentId, ct))
                throw new NotFoundException("Inspection", inspectionId);
        }
        else if (inspection.Status is InspectionStatus.Draft)
        {
            throw new NotFoundException("Inspection", inspectionId); // never published: agents must not learn it exists
        }
        else if (inspection.Status != InspectionStatus.Open && inspection.AgentId != agentId)
        {
            throw new DomainRuleException("inspection.not_available", "This inspection has already been accepted or is not available.");
        }

        inspection.Accept(agentId, clock.UtcNow);
        audit.Record(AuditActions.InspectionAccepted, nameof(Inspection), inspection.Id);
        var companyUsers = await db.CompanyMembers.AsNoTracking().Where(m => m.CompanyId == inspection.CompanyId).Select(m => m.UserId).ToListAsync(ct);
        foreach (var userId in companyUsers)
            notifications.Enqueue(new NotificationMessage(NotificationTypes.InspectionAccepted, "An agent accepted your inspection",
                "Your inspection has been accepted and is now assigned.", RecipientUserId: userId,
                Link: urls.Value.Web($"/company/inspections/{inspection.Id}")));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Another agent accepted this inspection moments ago.", "inspection.already_accepted");
        }
        return await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct);
    }

    // ---------- My inspections ----------

    public async Task<AgentDashboardDto> DashboardAsync(CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var now = clock.UtcNow;
        var available = await db.Inspections.CountAsync(i => i.Status == InspectionStatus.Open && i.Visibility == InspectionVisibility.Public &&
                                                             i.AgentId == null && (i.AcceptBy == null || i.AcceptBy > now), ct);
        var mine = await db.Inspections.Where(i => i.AgentId == agentId).GroupBy(i => i.Status)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int C(params InspectionStatus[] s) => mine.Where(x => s.Contains(x.Key)).Sum(x => x.Count);
        var active = await ListMineAsync(completed: false, ct);
        return new AgentDashboardDto(available, C(InspectionStatus.Assigned), C(InspectionStatus.InProgress), C(InspectionStatus.Review),
            C(InspectionStatus.Completed, InspectionStatus.AwaitingTenant, InspectionStatus.Accepted, InspectionStatus.Disputed), active);
    }

    public async Task<IReadOnlyList<InspectionSummaryDto>> ListMineAsync(bool completed, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var query = db.Inspections.AsNoTracking().Where(i => i.AgentId == agentId);
        query = completed
            ? query.Where(i => i.Status == InspectionStatus.Completed || i.Status == InspectionStatus.AwaitingTenant ||
                               i.Status == InspectionStatus.Accepted || i.Status == InspectionStatus.Disputed)
            : query.Where(i => i.Status == InspectionStatus.Assigned || i.Status == InspectionStatus.InProgress || i.Status == InspectionStatus.Review);
        return await inspections.ProjectSummaries(query.OrderByDescending(i => i.UpdatedAt)).ToListAsync(ct);
    }

    public async Task<InspectionDetailsDto> GetAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct, tracking: false);
        return await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct);
    }

    public async Task<InspectionDetailsDto> StartAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        inspection.Start(agentId, clock.UtcNow);
        audit.Record(AuditActions.InspectionStarted, nameof(Inspection), inspection.Id);
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct);
    }

    // ---------- Rooms ----------

    public async Task<RoomDetailDto> GetRoomAsync(Guid inspectionId, Guid roomId, CancellationToken ct)
    {
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct, tracking: false);
        return await BuildRoomAsync(inspection, roomId, ct);
    }

    public Task<RoomDetailDto> UpdateRoomAsync(Guid inspectionId, Guid roomId, UpdateRoomRequest request, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (agentId, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var now = clock.UtcNow;
            var previousEdit = room.FinalDescriptionEditedAt;
            var previousSource = room.FinalDescriptionSource;
            if (room.EditFinalDescription(request.FinalDescription, agentId, now) &&
                (previousEdit is null || now - previousEdit > TimeSpan.FromMinutes(2)))
            {
                // Autosave produces many small edits; one audit entry per editing session is enough.
                audit.Record(AuditActions.DescriptionEdited, nameof(InspectionRoom), room.Id,
                    new { inspectionId, target = "room", previousSource = previousSource.ToString(), length = room.FinalDescription?.Length ?? 0 });
            }
            if (request.DefectsFound != room.DefectsFound) room.SetDefectsFound(request.DefectsFound, now);
            room.SetAgentNotes(request.AgentNotes, now);
            await ReopenIfIncompleteAsync(inspection, room, now, ct);
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    public Task<RoomDetailDto> CompleteRoomAsync(Guid inspectionId, Guid roomId, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (_, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var context = await contexts.LoadForRoomAsync(inspection, roomId, ct);
            var now = clock.UtcNow;
            room.Complete(context, now);
            audit.Record(AuditActions.RoomCompleted, nameof(InspectionRoom), room.Id, new { inspectionId });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    public Task<RoomDetailDto> ReopenRoomAsync(Guid inspectionId, Guid roomId, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (_, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            room.Reopen(clock.UtcNow);
            audit.Record(AuditActions.RoomReopened, nameof(InspectionRoom), room.Id, new { inspectionId });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    // ---------- Defects ----------

    public Task<RoomDetailDto> AddDefectAsync(Guid inspectionId, Guid roomId, AddDefectRequest request, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (agentId, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var defect = room.AddDefect(request.Description, request.Location, agentId, clock.UtcNow);
            db.InspectionDefects.Add(defect);
            room.Reopen(clock.UtcNow); // a new defect is never complete (no photo, not confirmed)
            audit.Record(AuditActions.DefectRecorded, nameof(InspectionDefect), defect.Id, new { inspectionId, roomId });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    public Task<RoomDetailDto> UpdateDefectAsync(Guid inspectionId, Guid roomId, Guid defectId, UpdateDefectRequest request, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (_, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var defect = room.GetDefect(defectId);
            var now = clock.UtcNow;
            var textChanged = defect.FinalDescription != request.FinalDescription?.Trim();
            defect.Update(request.Description, request.Location, request.FinalDescription, request.Classification, request.AgentConfirmed, now);
            room.MarkActivity(now);
            await ReopenIfIncompleteAsync(inspection, room, now, ct);
            audit.Record(textChanged ? AuditActions.DescriptionEdited : AuditActions.DefectUpdated, nameof(InspectionDefect), defect.Id,
                new { inspectionId, roomId, target = "defect", classification = defect.Classification.ToString(), confirmed = defect.AgentConfirmed });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    public Task<RoomDetailDto> RemoveDefectAsync(Guid inspectionId, Guid roomId, Guid defectId, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (_, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var now = clock.UtcNow;
            room.RemoveDefect(defectId, now);
            if (room.Status == InspectionRoomStatus.Completed) room.Reopen(now);
            var photos = await db.InspectionRoomMedia.Where(m => m.DefectId == defectId).ToListAsync(ct);
            db.InspectionRoomMedia.RemoveRange(photos);
            audit.Record(AuditActions.DefectRemoved, nameof(InspectionDefect), defectId, new { inspectionId, roomId, photos = photos.Count });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    // ---------- Move Out comparison ----------

    public Task<RoomDetailDto> RunBasicComparisonAsync(Guid inspectionId, Guid roomId, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (_, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var comparison = await db.InspectionComparisons.FirstOrDefaultAsync(c => c.InspectionRoomId == roomId, ct)
                             ?? throw new DomainRuleException("comparison.none", "This room has no baseline inspection to compare with.");
            var baseline = await snapshots.FindRoomAsync(comparison.SourceInspectionId, comparison.SourceInspectionRoomId, ct)
                           ?? throw new DomainRuleException("comparison.baseline_missing", "The baseline report could not be found.");

            var baselineAi = await LoadRoomAnalysisAsync(baseline.AiAnalysisId, ct);
            var currentAi = await LoadRoomAnalysisAsync(room.LatestAiAnalysisId, ct);
            var result = BasicComparisonBuilder.Build(baseline, baselineAi, room.FinalDescription, room.Defects, currentAi);
            var now = clock.UtcNow;
            comparison.BasicComparison = result.Text;
            comparison.UpdatedAt = now;
            room.MarkActivity(now);
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    public Task<RoomDetailDto> RecordComparisonDecisionAsync(Guid inspectionId, Guid roomId, ComparisonDecisionRequest request, CancellationToken ct)
    {
        return writeScope.RunAsync(inspectionId, async () =>
        {
            var (agentId, inspection, room) = await LoadEditableRoomAsync(inspectionId, roomId, ct);
            var comparison = await db.InspectionComparisons.FirstOrDefaultAsync(c => c.InspectionRoomId == roomId, ct)
                             ?? throw new DomainRuleException("comparison.none", "This room has no baseline inspection to compare with.");
            var now = clock.UtcNow;
            comparison.RecordDecision(request.Decision, request.Notes, agentId, now);
            room.MarkActivity(now);
            audit.Record(AuditActions.ComparisonDecisionRecorded, nameof(InspectionComparison), comparison.Id,
                new { inspectionId, roomId, decision = request.Decision.ToString() });
            await SaveAsync(ct);
            return await BuildRoomAsync(inspection, roomId, ct);
        }, ct);
    }

    // ---------- Review ----------

    public async Task<InspectionDetailsDto> SubmitForReviewAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        var issues = InspectionReadiness.GetBlockingIssues(inspection, await contexts.LoadAsync(inspection, ct), requireRoomsCompleted: true);
        inspection.SubmitForReview(agentId, issues, clock.UtcNow);
        audit.Record(AuditActions.InspectionSubmittedForReview, nameof(Inspection), inspection.Id);
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct);
    }

    public async Task<InspectionDetailsDto> ReturnToInProgressAsync(Guid inspectionId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        inspection.ReturnToInProgress(agentId, clock.UtcNow);
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Agent, ct);
    }

    // ---------- helpers ----------

    /// <summary>Keeps "Completed" honest: an edit that breaks a completion rule moves the room back to InProgress.</summary>
    private async Task ReopenIfIncompleteAsync(Inspection inspection, InspectionRoom room, DateTimeOffset now, CancellationToken ct)
    {
        if (room.Status != InspectionRoomStatus.Completed) return;
        var context = await contexts.LoadForRoomAsync(inspection, room.Id, ct);
        if (room.GetCompletionIssues(context).Count > 0) room.Reopen(now);
    }

    private async Task<(Guid AgentId, Inspection Inspection, InspectionRoom Room)> LoadEditableRoomAsync(Guid inspectionId, Guid roomId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        inspection.EnsureEditableBy(agentId);
        var room = inspection.Rooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException("Room", roomId);
        return (agentId, inspection, room);
    }

    private async Task<RoomDetailDto> BuildRoomAsync(Inspection inspection, Guid roomId, CancellationToken ct)
    {
        var room = inspection.Rooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException("Room", roomId);
        var media = await db.InspectionRoomMedia.AsNoTracking().Where(m => m.InspectionRoomId == roomId)
            .OrderBy(m => m.UploadedAt).ToListAsync(ct);
        var analyses = await db.AiAnalyses.AsNoTracking().Where(a => a.InspectionRoomId == roomId)
            .OrderByDescending(a => a.RequestedAt).ToListAsync(ct);

        var general = new List<MediaDto>();
        foreach (var m in media.Where(m => m.MediaType == MediaType.General)) general.Add(await mediaUrls.ToDtoAsync(m, ct));

        var defects = new List<DefectDto>();
        foreach (var d in room.Defects.OrderBy(d => d.CreatedAt))
        {
            var photos = new List<MediaDto>();
            foreach (var m in media.Where(m => m.DefectId == d.Id)) photos.Add(await mediaUrls.ToDtoAsync(m, ct));
            var latest = analyses.FirstOrDefault(a => a.Kind == AiAnalysisKind.DefectDescription && a.DefectId == d.Id);
            defects.Add(new DefectDto(d.Id, d.Description, d.Location, d.Classification.ToString(), d.AIDescription, d.AIConfidence,
                d.FinalDescription, d.AgentConfirmed, photos, latest is null ? null : AiAnalysisService.ToDto(latest, analyzer.IsMock)));
        }

        ComparisonDto? comparisonDto = null;
        var comparison = await db.InspectionComparisons.AsNoTracking().FirstOrDefaultAsync(c => c.InspectionRoomId == roomId, ct);
        if (comparison is not null)
        {
            var baselineSnapshot = await snapshots.GetLatestForInspectionAsync(comparison.SourceInspectionId, ct);
            var baselineRoom = baselineSnapshot?.Rooms.FirstOrDefault(r => r.Id == comparison.SourceInspectionRoomId);
            BaselineRoomDto? baseline = null;
            if (baselineRoom is not null)
            {
                var photoUrls = new List<string>();
                foreach (var p in baselineRoom.Photos) photoUrls.Add(await mediaUrls.GetUrlAsync(p.StorageKey, ct));
                var baselineDefects = new List<BaselineDefectDto>();
                foreach (var d in baselineRoom.Defects)
                {
                    var urlsForDefect = new List<string>();
                    foreach (var p in d.Photos) urlsForDefect.Add(await mediaUrls.GetUrlAsync(p.StorageKey, ct));
                    baselineDefects.Add(new BaselineDefectDto(d.Title, d.Location, d.Classification, d.Description, urlsForDefect));
                }
                baseline = new BaselineRoomDto(baselineRoom.Name, baselineRoom.Description, baselineRoom.AgentNotes, baselineRoom.DefectsFound,
                    photoUrls, baselineDefects, baselineSnapshot!.ReportNumber, baselineSnapshot.Inspection.CompletedAt);
            }
            var latestComparison = analyses.FirstOrDefault(a => a.Kind == AiAnalysisKind.RoomComparison);
            comparisonDto = new ComparisonDto(comparison.Id, comparison.BasicComparison, comparison.AIAnalysis,
                comparison.AgentDecision?.ToString(), comparison.AgentNotes, comparison.DecidedAt,
                latestComparison is null ? null : AiAnalysisService.ToDto(latestComparison, analyzer.IsMock), baseline);
        }

        var context = await contexts.LoadForRoomAsync(inspection, roomId, ct);
        var ordered = inspection.Rooms.OrderBy(r => r.Sequence).ToList();
        var index = ordered.FindIndex(r => r.Id == roomId);
        var latestRoomAnalysis = analyses.FirstOrDefault(a => a.Kind == AiAnalysisKind.RoomDescription);

        return new RoomDetailDto(
            room.Id, inspection.Id, room.Name, room.RoomType.ToString(), room.Sequence, room.Status.ToString(), room.IsRequired,
            room.AiDescription, room.AiDescriptionGeneratedAt, room.FinalDescription, room.FinalDescriptionSource.ToString(),
            room.DefectsFound, room.AgentNotes, room.CompletedAt, general, defects,
            latestRoomAnalysis is null ? null : AiAnalysisService.ToDto(latestRoomAnalysis, analyzer.IsMock),
            comparisonDto, room.GetCompletionIssues(context),
            InspectionStateMachine.IsEditable(inspection.Status), inspection.Status.ToString(),
            index > 0 ? ordered[index - 1].Id : null,
            index < ordered.Count - 1 ? ordered[index + 1].Id : null,
            ordered.Count(r => r.Status == InspectionRoomStatus.Completed), ordered.Count);
    }

    private async Task<RoomAnalysisResult?> LoadRoomAnalysisAsync(Guid? analysisId, CancellationToken ct)
    {
        if (analysisId is null) return null;
        var json = await db.AiAnalyses.AsNoTracking().Where(a => a.Id == analysisId && a.Status == AiAnalysisStatus.Completed)
            .Select(a => a.ResultJson).FirstOrDefaultAsync(ct);
        return json is null ? null : JsonSerializer.Deserialize<RoomAnalysisResult>(json, AiAnalysisProcessor.Json);
    }

    private Task<bool> HasVerifiedInvitationAsync(Guid inspectionId, Guid agentId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.InspectionInvitations.AnyAsync(x => x.InspectionId == inspectionId && x.UsedByUserId == agentId &&
                                                      x.RevokedAt == null && x.ExpiresAt > now, ct);
    }

    private static AvailableInspectionDto ToAvailable(Inspection i, string companyName, string city, string postcode, string propertyType,
        IReadOnlyList<string> rooms) =>
        new(i.Id, i.InspectionType.ToString(), i.Visibility.ToString(), companyName, city, InspectionDetailsBuilder.PostcodeArea(postcode),
            propertyType, rooms.Count, rooms, i.ScheduledDate, i.AcceptBy, i.PublishedAt, i.ComparisonInspectionId is not null);

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The inspection was changed at the same time. Refresh and try again.");
        }
    }
}

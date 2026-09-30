using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Company-side inspection use cases: create, publish (room snapshot), invitations, cancel, send to tenant.</summary>
public sealed class InspectionService(
    IAppDbContext db,
    CompanyAccess companyAccess,
    InspectionAccess access,
    InspectionDetailsBuilder details,
    IAccessCodeHasher codeHasher,
    INotificationService notifications,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<AppUrlOptions> urls,
    IOptions<InspectionRulesOptions> rules)
{
    public async Task<IReadOnlyList<InspectionSummaryDto>> ListAsync(Guid? propertyId, InspectionStatus? status, CancellationToken ct)
    {
        var companyId = await companyAccess.RequireAsync(CompanyPermission.View, ct);
        var query = db.Inspections.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (propertyId is not null) query = query.Where(i => i.PropertyId == propertyId);
        if (status is not null) query = query.Where(i => i.Status == status);
        return await ProjectSummaries(query.OrderByDescending(i => i.UpdatedAt)).ToListAsync(ct);
    }

    public async Task<InspectionDetailsDto> GetAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.View, ct, tracking: false);
        return await details.BuildAsync(inspection, InspectionViewerKind.Company, ct);
    }

    public async Task<PublishResultDto> CreateAsync(CreateInspectionRequest request, CancellationToken ct)
    {
        var companyId = await companyAccess.RequireAsync(CompanyPermission.ManageInspections, ct);
        var userId = currentUser.RequireUserId();

        var property = await db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.CompanyId == companyId, ct)
                       ?? throw new NotFoundException(EntityNames.Property, request.PropertyId);
        if (request.TenancyId is not null &&
            !await db.Tenancies.AnyAsync(t => t.Id == request.TenancyId && t.PropertyId == property.Id && t.CompanyId == companyId, ct))
            throw new ValidationException("TenancyId", new($"Tenancy {request.TenancyId} does not belong to this property.", $"A locação {request.TenancyId} não pertence a este imóvel."));

        if (request.ComparisonInspectionId is not null)
            await ValidateComparisonAsync(request, companyId, ct);

        var now = clock.UtcNow;
        var inspection = Inspection.CreateDraft(companyId, property.Id, request.TenancyId, request.InspectionType,
            request.Visibility, request.ComparisonInspectionId, request.Instructions, request.ScheduledDate, request.AcceptBy, userId, now);
        db.Inspections.Add(inspection);
        audit.Record(AuditActions.InspectionCreated, nameof(Inspection), inspection.Id,
            new { type = inspection.InspectionType.ToString(), visibility = inspection.Visibility.ToString(), propertyId = property.Id, comparisonInspectionId = request.ComparisonInspectionId });
        await db.SaveChangesAsync(ct);

        if (!request.PublishNow)
            return new PublishResultDto(await details.BuildAsync(inspection, InspectionViewerKind.Company, ct), null);
        return await PublishAsync(inspection.Id, request.InviteEmail, ct);
    }

    public async Task<InspectionDetailsDto> UpdateDraftAsync(Guid inspectionId, UpdateDraftRequest request, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        inspection.UpdateDraft(request.Visibility, request.Instructions, request.ScheduledDate, request.AcceptBy, clock.UtcNow);
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Company, ct);
    }

    /// <summary>Draft → Open. Snapshots the property's current rooms into the inspection.</summary>
    public async Task<PublishResultDto> PublishAsync(Guid inspectionId, string? inviteEmail, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        var now = clock.UtcNow;

        var propertyRooms = await db.PropertyRooms.AsNoTracking()
            .Where(r => r.PropertyId == inspection.PropertyId && r.ArchivedAt == null)
            .OrderBy(r => r.Sequence).ToListAsync(ct);

        // Map each property room to the baseline inspection's snapshot room (same original property room).
        var baselineRooms = new Dictionary<Guid, Guid>();
        if (inspection.ComparisonInspectionId is not null)
        {
            baselineRooms = await db.InspectionRooms.AsNoTracking()
                .Where(r => r.InspectionId == inspection.ComparisonInspectionId)
                .ToDictionaryAsync(r => r.OriginalPropertyRoomId, r => r.Id, ct);
        }

        var sources = propertyRooms.Select(r => new RoomSnapshotSource(r.Id, r.RoomType, r.Name, r.Sequence,
            baselineRooms.TryGetValue(r.Id, out var baselineRoomId) ? baselineRoomId : null)).ToList();
        inspection.Publish(sources, now);

        if (inspection.ComparisonInspectionId is { } sourceInspectionId)
        {
            foreach (var room in inspection.Rooms.Where(r => r.ComparisonInspectionRoomId is not null))
            {
                db.InspectionComparisons.Add(new InspectionComparison
                {
                    Id = Guid.NewGuid(),
                    SourceInspectionId = sourceInspectionId,
                    TargetInspectionId = inspection.Id,
                    InspectionRoomId = room.Id,
                    SourceInspectionRoomId = room.ComparisonInspectionRoomId!.Value,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        PrivateInvitationDto? invitation = null;
        if (inspection.Visibility == InspectionVisibility.Private)
            invitation = CreateInvitation(inspection, inviteEmail, now);

        audit.Record(AuditActions.InspectionPublished, nameof(Inspection), inspection.Id,
            new { rooms = inspection.Rooms.Count, visibility = inspection.Visibility.ToString() });
        await SaveAsync(ct);
        return new PublishResultDto(await details.BuildAsync(inspection, InspectionViewerKind.Company, ct), invitation);
    }

    public async Task<PrivateInvitationDto> RegenerateInvitationAsync(Guid inspectionId, string? inviteEmail, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        if (inspection.Visibility != InspectionVisibility.Private || inspection.Status != InspectionStatus.Open)
            throw new DomainRuleException("invitation.not_applicable", new("Invitations can only be regenerated for open private inspections.", "Convites só podem ser gerados novamente para vistorias privadas abertas."));
        var now = clock.UtcNow;
        var previous = await db.InspectionInvitations.Where(x => x.InspectionId == inspectionId && x.RevokedAt == null).ToListAsync(ct);
        foreach (var inv in previous) inv.Revoke(now);
        var invitation = CreateInvitation(inspection, inviteEmail, now);
        await SaveAsync(ct);
        return invitation;
    }

    public async Task<InspectionDetailsDto> CancelAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        inspection.Cancel(currentUser.RequireUserId(), clock.UtcNow);
        audit.Record(AuditActions.InspectionCancelled, nameof(Inspection), inspection.Id);
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Company, ct);
    }

    /// <summary>Completed → AwaitingTenant when tenants were added after finalization.</summary>
    public async Task<InspectionDetailsDto> SendToTenantAsync(Guid inspectionId, CancellationToken ct)
    {
        var inspection = await access.GetForCompanyAsync(inspectionId, CompanyPermission.ManageInspections, ct);
        var members = inspection.TenancyId is null ? [] :
            await db.TenancyMembers.AsNoTracking().Where(m => m.TenancyId == inspection.TenancyId).ToListAsync(ct);
        if (members.Count == 0)
            throw new DomainRuleException("tenancy.no_tenants", new("Add at least one tenant to the tenancy first.", "Adicione primeiro pelo menos um inquilino à locação."));
        inspection.SendToTenant(clock.UtcNow);
        TenantNotifications.NotifyReportReady(notifications, urls.Value, inspection, members);
        audit.Record(AuditActions.InspectionSentToTenant, nameof(Inspection), inspection.Id, new { tenants = members.Count });
        await SaveAsync(ct);
        return await details.BuildAsync(inspection, InspectionViewerKind.Company, ct);
    }

    /// <summary>Finalized Move In inspections of a property that a new Move Out can be compared with.</summary>
    public async Task<IReadOnlyList<InspectionSummaryDto>> ListComparisonCandidatesAsync(Guid propertyId, CancellationToken ct)
        // Candidates of all tenancies are listed; the client filters by the selected tenancy and the server validates it.
    {
        var companyId = await companyAccess.RequireAsync(CompanyPermission.View, ct);
        var query = db.Inspections.AsNoTracking().Where(i => i.CompanyId == companyId && i.PropertyId == propertyId &&
            i.InspectionType == InspectionType.MoveIn &&
            (i.Status == InspectionStatus.Completed || i.Status == InspectionStatus.AwaitingTenant ||
             i.Status == InspectionStatus.Accepted || i.Status == InspectionStatus.Disputed));
        return await ProjectSummaries(query.OrderByDescending(i => i.CompletedAt)).ToListAsync(ct);
    }

    private async Task ValidateComparisonAsync(CreateInspectionRequest request, Guid companyId, CancellationToken ct)
    {
        var source = await db.Inspections.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.ComparisonInspectionId && i.CompanyId == companyId, ct)
                     ?? throw new ValidationException("ComparisonInspectionId", new($"The comparison inspection {request.ComparisonInspectionId} was not found.", $"A vistoria de comparação {request.ComparisonInspectionId} não foi encontrada."));
        if (source.PropertyId != request.PropertyId)
            throw new ValidationException("ComparisonInspectionId", new("The comparison inspection must be for the same property.", "A vistoria de comparação deve ser do mesmo imóvel."));
        if (!source.IsFinalized)
            throw new ValidationException("ComparisonInspectionId", new("The comparison inspection must be finalized.", "A vistoria de comparação precisa estar finalizada."));
        if (request.InspectionType == InspectionType.MoveOut && source.InspectionType != InspectionType.MoveIn)
            throw new ValidationException("ComparisonInspectionId", new("A Move Out inspection must be compared with a Move In inspection.", "Uma vistoria de saída deve ser comparada com uma vistoria de entrada."));
        // A baseline from another tenancy would expose previous tenants' records to the current ones.
        if (source.TenancyId != request.TenancyId)
            throw new ValidationException("ComparisonInspectionId", new("The comparison inspection belongs to a different tenancy.", "A vistoria de comparação pertence a outra locação."));
    }

    private PrivateInvitationDto CreateInvitation(Inspection inspection, string? inviteEmail, DateTimeOffset now)
    {
        var token = SecureTokens.Create();
        var code = SecureTokens.CreateNumericCode(6);
        var invitation = new InspectionInvitation
        {
            Id = Guid.NewGuid(),
            InspectionId = inspection.Id,
            TokenHash = SecureTokens.Sha256Hex(token),
            AccessCodeHash = codeHasher.Hash(code),
            InvitedEmail = string.IsNullOrWhiteSpace(inviteEmail) ? null : inviteEmail.Trim(),
            CreatedAt = now,
            CreatedBy = currentUser.RequireUserId(),
            ExpiresAt = now.AddDays(rules.Value.PrivateInvitationDays),
            MaxAttempts = InspectionInvitation.DefaultMaxAttempts,
        };
        db.InspectionInvitations.Add(invitation);
        var link = urls.Value.Web($"/inspection/invite/{token}");
        if (invitation.InvitedEmail is not null)
        {
            // The code is deliberately NOT included next to the link: the company shares it through a separate channel.
            notifications.Enqueue(new NotificationMessage(NotificationTypes.InspectionInvitation,
                "You have been invited to carry out an inspection",
                "A company invited you to a private inspection. Open the link and enter the 6-digit access code the company shares with you separately.",
                RecipientEmail: invitation.InvitedEmail, TransientLink: link));
        }
        audit.Record(AuditActions.InspectionInvitationCreated, nameof(Inspection), inspection.Id,
            new { invitationId = invitation.Id, expiresAt = invitation.ExpiresAt });
        return new PrivateInvitationDto(link, code, invitation.ExpiresAt, invitation.MaxAttempts);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(InspectionMessages.ChangedConcurrently);
        }
    }

    internal IQueryable<InspectionSummaryDto> ProjectSummaries(IQueryable<Inspection> query) =>
        from i in query
        join p in db.Properties on i.PropertyId equals p.Id
        join u in db.Users on i.AgentId equals u.Id into agents
        from agent in agents.DefaultIfEmpty()
        select new InspectionSummaryDto(
            i.Id, i.PropertyId, p.AddressLine1 + ", " + p.City + " " + p.Postcode, i.TenancyId,
            i.InspectionType.ToString(), i.Visibility.ToString(), i.Status.ToString(),
            agent != null ? agent.FullName : null,
            i.Rooms.Count(r => r.Status == InspectionRoomStatus.Completed), i.Rooms.Count,
            i.ScheduledDate, i.CreatedAt, i.UpdatedAt, i.CompletedAt);
}

internal static class TenantNotifications
{
    public static void NotifyReportReady(INotificationService notifications, AppUrlOptions urls, Inspection inspection,
        IEnumerable<Tenancies.Domain.TenancyMember> members)
    {
        foreach (var m in members)
        {
            notifications.Enqueue(new NotificationMessage(
                Notifications.Application.NotificationTypes.ReportAwaitingTenant,
                "Your inspection report is ready for review",
                $"The {inspection.InspectionType} inspection report is ready. Please review it, add any observations and confirm or dispute it.",
                RecipientUserId: m.UserId, RecipientEmail: m.Email,
                Link: urls.Web(m.UserId is null ? "/login" : $"/tenant/inspections/{inspection.Id}")));
        }
    }
}

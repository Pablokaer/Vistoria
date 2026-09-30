using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Properties.Application;

public sealed record RoomInput(RoomType RoomType, string Name);

public sealed record CreatePropertyRequest(
    string AddressLine1, string? AddressLine2, string City, string Postcode, string Country,
    PropertyType PropertyType, IReadOnlyList<RoomInput>? Rooms);

public sealed record UpdatePropertyRequest(
    string AddressLine1, string? AddressLine2, string City, string Postcode, string Country, PropertyType PropertyType);

public sealed record ReorderRoomsRequest(IReadOnlyList<Guid> RoomIds);

public sealed record PropertyRoomDto(Guid Id, string RoomType, string Name, int Sequence, DateTimeOffset CreatedAt);

public sealed record PropertySummaryDto(Guid Id, string AddressLine1, string? AddressLine2, string City, string Postcode,
    string Country, string PropertyType, int RoomCount, int ActiveTenancies, int OpenInspections, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record PropertyDto(Guid Id, string AddressLine1, string? AddressLine2, string City, string Postcode,
    string Country, string PropertyType, IReadOnlyList<PropertyRoomDto> Rooms, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed class PropertyService(IAppDbContext db, CompanyAccess access, IAuditLogger audit, IClock clock)
{
    public async Task<IReadOnlyList<PropertySummaryDto>> ListAsync(string? search, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.View, ct);
        var query = db.Properties.AsNoTracking().Where(p => p.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.AddressLine1.ToLower().Contains(term) || p.City.ToLower().Contains(term) ||
                                     p.Postcode.ToLower().Contains(term));
        }

        var rows = await query.OrderBy(p => p.AddressLine1)
            .Select(p => new
            {
                p,
                Rooms = p.Rooms.Count(r => r.ArchivedAt == null),
                Tenancies = db.Tenancies.Count(t => t.PropertyId == p.Id && (t.Status == Tenancies.Domain.TenancyStatus.Active || t.Status == Tenancies.Domain.TenancyStatus.Upcoming)),
                Open = db.Inspections.Count(i => i.PropertyId == p.Id &&
                    i.Status != Inspections.Domain.InspectionStatus.Accepted && i.Status != Inspections.Domain.InspectionStatus.Cancelled &&
                    i.Status != Inspections.Domain.InspectionStatus.Expired && i.Status != Inspections.Domain.InspectionStatus.Disputed &&
                    i.Status != Inspections.Domain.InspectionStatus.Completed),
            })
            .ToListAsync(ct);
        return rows.Select(r => new PropertySummaryDto(r.p.Id, r.p.AddressLine1, r.p.AddressLine2, r.p.City, r.p.Postcode,
            r.p.Country, r.p.PropertyType.ToString(), r.Rooms, r.Tenancies, r.Open, r.p.CreatedAt, r.p.UpdatedAt)).ToList();
    }

    public async Task<PropertyDto> GetAsync(Guid propertyId, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.View, ct);
        var property = await db.Properties.AsNoTracking().Include(p => p.Rooms)
                           .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId, ct)
                       ?? throw new NotFoundException("Property", propertyId);
        return ToDto(property);
    }

    public async Task<PropertyDto> CreateAsync(CreatePropertyRequest request, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.ManageProperties, ct);
        var now = clock.UtcNow;
        var property = Property.Create(companyId, request.AddressLine1, request.AddressLine2, request.City,
            request.Postcode, request.Country, request.PropertyType, now);
        foreach (var room in request.Rooms ?? [])
            property.AddRoom(room.RoomType, room.Name, now);

        db.Properties.Add(property);
        audit.Record(AuditActions.PropertyCreated, nameof(Property), property.Id,
            new { rooms = property.Rooms.Count, type = property.PropertyType.ToString() });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    public async Task<PropertyDto> UpdateAsync(Guid propertyId, UpdatePropertyRequest request, CancellationToken ct)
    {
        var property = await LoadForEditAsync(propertyId, ct);
        property.Update(request.AddressLine1, request.AddressLine2, request.City, request.Postcode, request.Country,
            request.PropertyType, clock.UtcNow);
        audit.Record(AuditActions.PropertyUpdated, nameof(Property), property.Id, new { change = "details" });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    public async Task<PropertyDto> AddRoomAsync(Guid propertyId, RoomInput input, CancellationToken ct)
    {
        var property = await LoadForEditAsync(propertyId, ct);
        var room = property.AddRoom(input.RoomType, input.Name, clock.UtcNow);
        db.PropertyRooms.Add(room);
        audit.Record(AuditActions.PropertyUpdated, nameof(Property), property.Id, new { change = "room_added", roomId = room.Id });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    public async Task<PropertyDto> UpdateRoomAsync(Guid propertyId, Guid roomId, RoomInput input, CancellationToken ct)
    {
        var property = await LoadForEditAsync(propertyId, ct);
        property.UpdateRoom(roomId, input.RoomType, input.Name, clock.UtcNow);
        audit.Record(AuditActions.PropertyUpdated, nameof(Property), property.Id, new { change = "room_updated", roomId });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    public async Task<PropertyDto> ArchiveRoomAsync(Guid propertyId, Guid roomId, CancellationToken ct)
    {
        var property = await LoadForEditAsync(propertyId, ct);
        property.ArchiveRoom(roomId, clock.UtcNow);
        audit.Record(AuditActions.PropertyUpdated, nameof(Property), property.Id, new { change = "room_removed", roomId });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    public async Task<PropertyDto> ReorderRoomsAsync(Guid propertyId, ReorderRoomsRequest request, CancellationToken ct)
    {
        var property = await LoadForEditAsync(propertyId, ct);
        property.ReorderRooms(request.RoomIds, clock.UtcNow);
        audit.Record(AuditActions.PropertyUpdated, nameof(Property), property.Id, new { change = "rooms_reordered" });
        await db.SaveChangesAsync(ct);
        return ToDto(property);
    }

    private async Task<Property> LoadForEditAsync(Guid propertyId, CancellationToken ct)
    {
        var companyId = await access.RequireAsync(CompanyPermission.ManageProperties, ct);
        return await db.Properties.Include(p => p.Rooms)
                   .FirstOrDefaultAsync(p => p.Id == propertyId && p.CompanyId == companyId, ct)
               ?? throw new NotFoundException("Property", propertyId);
    }

    public static PropertyDto ToDto(Property p) => new(p.Id, p.AddressLine1, p.AddressLine2, p.City, p.Postcode, p.Country,
        p.PropertyType.ToString(),
        p.ActiveRooms.Select(r => new PropertyRoomDto(r.Id, r.RoomType.ToString(), r.Name, r.Sequence, r.CreatedAt)).ToList(),
        p.CreatedAt, p.UpdatedAt);
}

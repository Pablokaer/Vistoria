using InspectFlow.Shared.Errors;

namespace InspectFlow.Modules.Properties.Domain;

public enum PropertyType
{
    House,
    Apartment,
    Studio,
    Townhouse,
    Bungalow,
    Commercial,
    Other,
}

public enum RoomType
{
    LivingRoom,
    Bedroom,
    Kitchen,
    Bathroom,
    DiningRoom,
    Hallway,
    Garage,
    Garden,
    Utility,
    Office,
    Balcony,
    Other,
}

public class Property
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string Postcode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public PropertyType PropertyType { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<PropertyRoom> Rooms { get; } = new();

    public static Property Create(Guid companyId, string addressLine1, string? addressLine2, string city,
        string postcode, string country, PropertyType type, DateTimeOffset now)
    {
        var property = new Property { Id = Guid.NewGuid(), CompanyId = companyId, CreatedAt = now };
        property.Update(addressLine1, addressLine2, city, postcode, country, type, now);
        return property;
    }

    public void Update(string addressLine1, string? addressLine2, string city, string postcode, string country,
        PropertyType type, DateTimeOffset now)
    {
        AddressLine1 = Required(addressLine1, nameof(AddressLine1), 200);
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = Required(city, nameof(City), 100);
        Postcode = Required(postcode, nameof(Postcode), 20).ToUpperInvariant();
        Country = Required(country, nameof(Country), 100);
        PropertyType = type;
        UpdatedAt = now;
    }

    public IEnumerable<PropertyRoom> ActiveRooms => Rooms.Where(r => r.ArchivedAt is null).OrderBy(r => r.Sequence);

    public PropertyRoom AddRoom(RoomType type, string name, DateTimeOffset now)
    {
        var trimmed = Required(name, "Name", 100);
        if (ActiveRooms.Any(r => string.Equals(r.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Name", $"A room named '{trimmed}' already exists in this property.");

        var sequence = Rooms.Count == 0 ? 1 : Rooms.Max(r => r.Sequence) + 1;
        var room = new PropertyRoom
        {
            Id = Guid.NewGuid(),
            PropertyId = Id,
            RoomType = type,
            Name = trimmed,
            Sequence = sequence,
            CreatedAt = now,
        };
        Rooms.Add(room);
        UpdatedAt = now;
        return room;
    }

    public void UpdateRoom(Guid roomId, RoomType type, string name, DateTimeOffset now)
    {
        var room = ActiveRooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException("Room", roomId);
        var trimmed = Required(name, "Name", 100);
        if (ActiveRooms.Any(r => r.Id != roomId && string.Equals(r.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("Name", $"A room named '{trimmed}' already exists in this property.");
        room.RoomType = type;
        room.Name = trimmed;
        UpdatedAt = now;
    }

    /// <summary>
    /// Rooms are archived rather than deleted so that historical inspections (which only hold
    /// a snapshot plus the original id) can still be matched for Move In / Move Out comparison.
    /// </summary>
    public void ArchiveRoom(Guid roomId, DateTimeOffset now)
    {
        var room = ActiveRooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException("Room", roomId);
        room.ArchivedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Re-orders active rooms. <paramref name="orderedRoomIds"/> must contain every active room exactly once.</summary>
    public void ReorderRooms(IReadOnlyList<Guid> orderedRoomIds, DateTimeOffset now)
    {
        var active = ActiveRooms.ToList();
        if (orderedRoomIds.Count != active.Count || orderedRoomIds.Distinct().Count() != active.Count ||
            active.Any(r => !orderedRoomIds.Contains(r.Id)))
            throw new ValidationException("RoomIds", "The new order must list every room exactly once.");

        for (var i = 0; i < orderedRoomIds.Count; i++)
            active.Single(r => r.Id == orderedRoomIds[i]).Sequence = i + 1;
        UpdatedAt = now;
    }

    public string FullAddress => string.Join(", ",
        new[] { AddressLine1, AddressLine2, City, Postcode, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException(field, $"{field} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength) throw new ValidationException(field, $"{field} must be at most {maxLength} characters.");
        return trimmed;
    }
}

/// <summary>A configurable room of a property (any number, any type).</summary>
public class PropertyRoom
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public RoomType RoomType { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}

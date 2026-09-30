using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Security;

namespace InspectFlow.Tests.Domain;

public class PropertyAndInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Property_supports_any_number_of_rooms_with_unique_names_and_sequence()
    {
        var property = Property.Create(Guid.NewGuid(), "12 Main Street", null, "Dublin", "d02 xy45", "Ireland", PropertyType.House, Now);
        for (var i = 1; i <= 12; i++) property.AddRoom(RoomType.Bedroom, $"Bedroom {i}", Now);
        Assert.Equal(12, property.ActiveRooms.Count());
        Assert.Equal(Enumerable.Range(1, 12), property.ActiveRooms.Select(r => r.Sequence));
        Assert.Equal("D02 XY45", property.Postcode);
        Assert.Throws<ValidationException>(() => property.AddRoom(RoomType.Bedroom, "bedroom 1", Now));
    }

    [Fact]
    public void Archived_rooms_are_hidden_but_kept()
    {
        var property = Property.Create(Guid.NewGuid(), "1 Road", null, "Cork", "T12", "Ireland", PropertyType.Apartment, Now);
        var kitchen = property.AddRoom(RoomType.Kitchen, "Kitchen", Now);
        property.AddRoom(RoomType.Bathroom, "Bathroom", Now);
        property.ArchiveRoom(kitchen.Id, Now);
        Assert.Single(property.ActiveRooms);
        Assert.Equal(2, property.Rooms.Count);
        // The name can be reused once the old room is archived.
        property.AddRoom(RoomType.Kitchen, "Kitchen", Now);
    }

    [Fact]
    public void Invitation_locks_after_max_attempts_and_is_single_use()
    {
        var invitation = new InspectionInvitation { Id = Guid.NewGuid(), ExpiresAt = Now.AddDays(1), MaxAttempts = 3 };
        var agent = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
        {
            invitation.EnsureUsable(agent, Now);
            invitation.RegisterFailedAttempt(Now);
        }
        Assert.Throws<TooManyAttemptsException>(() => invitation.EnsureUsable(agent, Now));

        var fresh = new InspectionInvitation { Id = Guid.NewGuid(), ExpiresAt = Now.AddDays(1) };
        fresh.MarkUsed(agent, Now);
        fresh.EnsureUsable(agent, Now); // same agent may come back
        Assert.Throws<DomainRuleException>(() => fresh.EnsureUsable(Guid.NewGuid(), Now));
        Assert.Throws<DomainRuleException>(() => fresh.EnsureUsable(agent, Now.AddDays(2))); // expired
    }

    [Fact]
    public void Access_codes_are_six_uniform_digits()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => SecureTokens.CreateNumericCode()).ToList();
        Assert.All(codes, c => Assert.Matches("^[0-9]{6}$", c));
        Assert.True(codes.Distinct().Count() > 190);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")]
    [InlineData(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, "image/webp")]
    [InlineData(new byte[] { (byte)'<', (byte)'s', (byte)'v', (byte)'g' }, null)]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 }, null)] // %PDF
    public void Upload_type_is_detected_from_content_not_from_the_declared_type(byte[] header, string? expected) =>
        Assert.Equal(expected, ImageFileValidator.DetectMimeType(header));

    [Theory]
    [InlineData("inspections/abc/rooms/def/x.jpg", true)]
    [InlineData("../etc/passwd", false)]
    [InlineData("/absolute/path.jpg", false)]
    [InlineData("inspections/a b.jpg", false)]
    public void Storage_keys_reject_traversal(string key, bool valid) => Assert.Equal(valid, StorageKeys.IsValid(key));
}

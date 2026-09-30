using System.Net;
using System.Text.Json.Nodes;

namespace InspectFlow.Tests.Support;

public static class TestData
{
    public const string Password = "Passw0rd!Test";

    public static readonly string[] RoomNames = ["Living Room", "Bedroom 1", "Bedroom 2", "Kitchen", "Bathroom", "Garage"];

    private static readonly Dictionary<string, string> RoomTypes = new()
    {
        ["Living Room"] = "LivingRoom", ["Bedroom 1"] = "Bedroom", ["Bedroom 2"] = "Bedroom",
        ["Kitchen"] = "Kitchen", ["Bathroom"] = "Bathroom", ["Garage"] = "Garage",
    };

    /// <summary>A minimal valid JPEG (magic bytes + filler); the mock AI provider does not decode images.</summary>
    public static byte[] Jpeg(int size = 2048, byte seed = 1)
    {
        var bytes = new byte[size];
        new Random(seed).NextBytes(bytes);
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF; bytes[3] = 0xE0;
        return bytes;
    }

    public static async Task<ApiClient> CompanyAsync(TestApp app, string? name = null)
    {
        var client = await app.CreateClient().RegisterAsync("Company");
        (await client.PostAsync("/api/companies", new { name = name ?? $"Company {Guid.NewGuid():N}"[..20] })).EnsureOk();
        // Re-login so the token reflects the workspace (not strictly needed: membership is resolved server-side).
        return client;
    }

    public static async Task<Guid> CreatePropertyAsync(ApiClient company, IEnumerable<string>? rooms = null)
    {
        var res = await company.PostAsync("/api/properties", new
        {
            addressLine1 = "12 Main Street",
            city = "Dublin",
            postcode = "D02 XY45",
            country = "Ireland",
            propertyType = "House",
            rooms = (rooms ?? RoomNames).Select(r => new { roomType = RoomTypes.GetValueOrDefault(r, "Other"), name = r }).ToArray(),
        });
        return res.EnsureOk().Id;
    }

    public static async Task<Guid> CreateTenancyAsync(ApiClient company, Guid propertyId) =>
        (await company.PostAsync("/api/tenancies", new { propertyId, startDate = "2026-01-01", endDate = "2027-01-01" })).EnsureOk().Id;

    /// <summary>Invites and joins a tenant. Returns the tenant's client.</summary>
    public static async Task<ApiClient> AddTenantAsync(TestApp app, ApiClient company, Guid tenancyId)
    {
        var tenant = await app.CreateClient().RegisterAsync("Tenant");
        var invite = (await company.PostAsync($"/api/tenancies/{tenancyId}/tenants", new { email = tenant.Email, fullName = "Tina Tenant" })).EnsureOk();
        var link = invite["invitationLink"].GetValue<string>();
        (await tenant.PostAsync($"/api/tenant/invitations/{link[(link.LastIndexOf('/') + 1)..]}/accept")).EnsureOk();
        return tenant;
    }

    public static async Task<JsonNode> CreateInspectionAsync(ApiClient company, Guid propertyId, Guid? tenancyId, string type = "MoveIn",
        string visibility = "Public", Guid? comparisonInspectionId = null, bool publish = true)
    {
        var res = await company.PostAsync("/api/inspections", new
        {
            propertyId,
            tenancyId,
            inspectionType = type,
            visibility,
            comparisonInspectionId,
            publishNow = publish,
        });
        return res.EnsureOk().Body!;
    }

    public static Guid InspectionId(JsonNode created) => Guid.Parse(created["inspection"]!["id"]!.GetValue<string>());

    /// <summary>Agent accepts and starts; returns room ids in order.</summary>
    public static async Task<List<Guid>> AcceptAndStartAsync(ApiClient agent, Guid inspectionId)
    {
        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
        var started = (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureOk();
        return started["rooms"].AsArray().Select(r => Guid.Parse(r!["id"]!.GetValue<string>())).ToList();
    }

    /// <summary>Upload one general photo, write a description and complete the room.</summary>
    public static async Task CompleteRoomAsync(ApiClient agent, Guid inspectionId, Guid roomId, string description = "Walls white, good visible condition.")
    {
        var baseUrl = $"/api/agent/inspections/{inspectionId}/rooms/{roomId}";
        (await agent.UploadAsync($"{baseUrl}/photos", Jpeg(), "room.jpg", "image/jpeg")).EnsureOk();
        (await agent.PutAsync(baseUrl, new { finalDescription = description, defectsFound = false, agentNotes = (string?)null })).EnsureOk();
        (await agent.PostAsync($"{baseUrl}/complete")).EnsureOk();
    }

    /// <summary>Complete every room, submit for review and finalize. Returns the finalize response.</summary>
    public static async Task<JsonNode> CompleteAndFinalizeAsync(ApiClient agent, Guid inspectionId, IEnumerable<Guid> roomIds)
    {
        foreach (var roomId in roomIds) await CompleteRoomAsync(agent, inspectionId, roomId);
        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/submit-review")).EnsureOk();
        return (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/finalize")).EnsureOk().Body!;
    }

    public static HttpStatusCode[] Rejected => [HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity];
}

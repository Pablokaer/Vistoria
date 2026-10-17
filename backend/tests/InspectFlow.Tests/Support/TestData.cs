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
        await Billing.SubscribeAsync(client);
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

    /// <summary>The company invites <paramref name="email"/> to the tenancy. Returns the invitation token (the last part of the link).</summary>
    public static async Task<string> InviteTenantAsync(ApiClient company, Guid tenancyId, string email)
    {
        var invite = (await company.PostAsync($"/api/tenancies/{tenancyId}/tenants", new { email, fullName = "Tina Tenant" })).EnsureOk();
        var link = invite["invitationLink"].GetValue<string>();
        return link[(link.LastIndexOf('/') + 1)..];
    }

    /// <summary>Invites a new tenant, who signs up from the invitation and so joins the tenancy. Returns the tenant's client.</summary>
    public static async Task<ApiClient> AddTenantAsync(TestApp app, ApiClient company, Guid tenancyId)
    {
        var email = $"tenant-{Guid.NewGuid():N}@test.local";
        var token = await InviteTenantAsync(company, tenancyId, email);
        return await app.CreateClient().RegisterAsync("Tenant", email, "Tina Tenant", token);
    }

    /// <summary>A tenant of a new company's tenancy, for tests that need some tenant account. Returns the tenant's client.</summary>
    public static async Task<ApiClient> TenantAsync(TestApp app)
    {
        var company = await CompanyAsync(app);
        var propertyId = await CreatePropertyAsync(company, ["Kitchen"]);
        return await AddTenantAsync(app, company, await CreateTenancyAsync(company, propertyId));
    }

    /// <summary>Public to the company's agents, or Direct to <paramref name="assignedAgentId"/>. Returns the inspection details.</summary>
    public static async Task<JsonNode> CreateInspectionAsync(ApiClient company, Guid propertyId, Guid? tenancyId, string type = "MoveIn",
        Guid? comparisonInspectionId = null, bool publish = true, Guid? assignedAgentId = null)
    {
        var res = await company.PostAsync("/api/inspections", new
        {
            propertyId,
            tenancyId,
            inspectionType = type,
            comparisonInspectionId,
            publishNow = publish,
            assignedAgentId,
        });
        return res.EnsureOk().Body!;
    }

    /// <summary>The company creates an agent account (agents cannot sign up themselves) and the agent signs in. Returns the agent's client.</summary>
    public static async Task<ApiClient> TeamAgentAsync(TestApp app, ApiClient company, string fullName = "Test Agent")
    {
        var email = $"agent-{Guid.NewGuid():N}@test.local";
        (await company.PostAsync("/api/companies/me/agents", new { email, fullName, password = Password })).EnsureOk();
        return await app.CreateClient().LoginAsync(email);
    }

    /// <summary>An agent of a different, newly created company: must never see the first company's inspections.</summary>
    public static async Task<ApiClient> OtherCompanyAgentAsync(TestApp app) => await TeamAgentAsync(app, await CompanyAsync(app));

    public static Guid InspectionId(JsonNode created) => Guid.Parse(created["id"]!.GetValue<string>());

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

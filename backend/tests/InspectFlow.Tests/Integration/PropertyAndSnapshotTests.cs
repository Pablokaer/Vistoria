using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PropertyAndSnapshotTests(TestApp app)
{
    [Fact]
    public async Task Company_creates_property_with_rooms_and_manages_them()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company);
        var property = (await company.GetAsync($"/api/properties/{propertyId}")).EnsureOk();
        Assert.Equal(TestData.RoomNames, property["rooms"].AsArray().Select(r => r!["name"]!.GetValue<string>()));

        var added = (await company.PostAsync($"/api/properties/{propertyId}/rooms", new { roomType = "Garden", name = "Back Garden" })).EnsureOk();
        Assert.Equal(7, added["rooms"].AsArray().Count);
        (await company.PostAsync($"/api/properties/{propertyId}/rooms", new { roomType = "Garden", name = "back garden" }))
            .EnsureStatus(HttpStatusCode.BadRequest);

        var garageId = added["rooms"].AsArray().First(r => r!["name"]!.GetValue<string>() == "Garage")!["id"]!.GetValue<string>();
        var afterRemove = (await company.DeleteAsync($"/api/properties/{propertyId}/rooms/{garageId}")).EnsureOk();
        Assert.DoesNotContain(afterRemove["rooms"].AsArray(), r => r!["name"]!.GetValue<string>() == "Garage");

        var updated = (await company.PutAsync($"/api/properties/{propertyId}",
            new { addressLine1 = "14 Main Street", city = "Dublin", postcode = "d02 xy45", country = "Ireland", propertyType = "Apartment" })).EnsureOk();
        Assert.Equal("14 Main Street", updated["addressLine1"].GetValue<string>());
        Assert.Equal("D02 XY45", updated["postcode"].GetValue<string>());
    }

    [Fact]
    public async Task Published_inspection_rooms_are_a_snapshot_unaffected_by_later_property_changes()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId);
        var inspectionId = TestData.InspectionId(created);
        Assert.Equal("Open", created["inspection"]!["status"]!.GetValue<string>());

        var property = (await company.GetAsync($"/api/properties/{propertyId}")).EnsureOk();
        var rooms = property["rooms"].AsArray();
        var bedroom1 = rooms.First(r => r!["name"]!.GetValue<string>() == "Bedroom 1")!["id"]!.GetValue<string>();
        var kitchen = rooms.First(r => r!["name"]!.GetValue<string>() == "Kitchen")!["id"]!.GetValue<string>();

        (await company.PutAsync($"/api/properties/{propertyId}/rooms/{bedroom1}", new { roomType = "Office", name = "Study" })).EnsureOk();
        (await company.DeleteAsync($"/api/properties/{propertyId}/rooms/{kitchen}")).EnsureOk();
        (await company.PostAsync($"/api/properties/{propertyId}/rooms", new { roomType = "Other", name = "Attic" })).EnsureOk();

        var inspection = (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk();
        Assert.Equal(TestData.RoomNames, inspection["rooms"].AsArray().Select(r => r!["name"]!.GetValue<string>()));
        Assert.Equal("Bedroom", inspection["rooms"].AsArray()[1]!["roomType"]!.GetValue<string>());
    }

    [Fact]
    public async Task Draft_is_not_visible_to_agents_and_publishing_requires_rooms()
    {
        var company = await TestData.CompanyAsync(app);
        var emptyProperty = await TestData.CreatePropertyAsync(company, rooms: []);
        var draft = TestData.InspectionId(await TestData.CreateInspectionAsync(company, emptyProperty, null, type: "Periodic", publish: false));

        var agent = await app.CreateClient().RegisterAsync("Agent");
        var available = (await agent.GetAsync("/api/agent/available")).EnsureOk();
        Assert.DoesNotContain(available.Body!.AsArray(), i => i!["id"]!.GetValue<string>() == draft.ToString());
        (await agent.PostAsync($"/api/agent/inspections/{draft}/accept")).EnsureStatus(HttpStatusCode.NotFound);

        var publish = await company.PostAsync($"/api/inspections/{draft}/publish");
        publish.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.no_rooms", publish.Code);
    }

    [Fact]
    public async Task Move_in_requires_a_tenancy_of_the_same_property()
    {
        var company = await TestData.CompanyAsync(app);
        var p1 = await TestData.CreatePropertyAsync(company);
        var p2 = await TestData.CreatePropertyAsync(company);
        var tenancyOfP2 = await TestData.CreateTenancyAsync(company, p2);
        (await company.PostAsync("/api/inspections", new { propertyId = p1, inspectionType = "MoveIn", visibility = "Public" })).EnsureStatus(HttpStatusCode.BadRequest);
        (await company.PostAsync("/api/inspections", new { propertyId = p1, tenancyId = tenancyOfP2, inspectionType = "MoveIn", visibility = "Public" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
    }
}

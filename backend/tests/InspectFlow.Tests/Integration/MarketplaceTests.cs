using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class MarketplaceTests(TestApp app)
{
    [Fact]
    public async Task Agent_sees_public_inspection_without_private_details_and_accepts_it()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        await TestData.AddTenantAsync(app, company, tenancyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));

        var agent = await app.CreateClient().RegisterAsync("Agent");
        var available = (await agent.GetAsync("/api/agent/available")).EnsureOk();
        var listed = available.Body!.AsArray().Single(i => i!["id"]!.GetValue<string>() == inspectionId.ToString())!;
        Assert.Equal("D02", listed["postcodeArea"]!.GetValue<string>());
        Assert.DoesNotContain("Main Street", listed.ToJsonString(), StringComparison.Ordinal);
        Assert.DoesNotContain("@", listed.ToJsonString(), StringComparison.Ordinal);

        // Not yet assigned: execution endpoints are hidden.
        (await agent.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);

        var accepted = (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
        Assert.Equal("Assigned", accepted["status"].GetValue<string>());
        Assert.Equal(agent.UserId.ToString(), accepted["agent"]["id"].GetValue<string>());
        Assert.True(accepted.Body!["acceptedAt"]!.GetValue<DateTimeOffset>() > DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.Equal("12 Main Street", accepted["property"]["addressLine1"].GetValue<string>());
        // Agents never see tenant emails.
        Assert.All(accepted["tenancy"]["members"].AsArray(), m => Assert.Equal("", m!["email"]!.GetValue<string>()));

        var after = (await agent.GetAsync("/api/agent/available")).EnsureOk();
        Assert.DoesNotContain(after.Body!.AsArray(), i => i!["id"]!.GetValue<string>() == inspectionId.ToString());

        var other = await app.CreateClient().RegisterAsync("Agent");
        var second = await other.PostAsync($"/api/agent/inspections/{inspectionId}/accept");
        Assert.Contains(second.Status, TestData.Rejected);
        (await other.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_accepts_result_in_exactly_one_assignment()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));

        var agents = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => app.CreateClient().RegisterAsync("Agent")));
        var results = await Task.WhenAll(agents.Select(a => a.PostAsync($"/api/agent/inspections/{inspectionId}/accept")));

        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
        Assert.All(results.Where(r => r.Status != HttpStatusCode.OK), r => Assert.Contains(r.Status, TestData.Rejected));

        var winner = agents[Array.FindIndex(results, r => r.Status == HttpStatusCode.OK)];
        var details = (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk();
        Assert.Equal(winner.UserId.ToString(), details["agent"]["id"].GetValue<string>());
        Assert.Equal("Assigned", details["status"].GetValue<string>());
    }

    [Fact]
    public async Task Agent_must_start_before_recording_and_other_agents_cannot_act()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var accepted = (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
        var roomId = accepted["rooms"].AsArray()[0]!["id"]!.GetValue<string>();

        var early = await agent.PutAsync($"/api/agent/inspections/{inspectionId}/rooms/{roomId}", new { finalDescription = "x", defectsFound = false });
        early.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.not_editable", early.Code);

        var intruder = await app.CreateClient().RegisterAsync("Agent");
        (await intruder.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureStatus(HttpStatusCode.NotFound);
        (await intruder.UploadAsync($"/api/agent/inspections/{inspectionId}/rooms/{roomId}/photos", TestData.Jpeg(), "a.jpg", "image/jpeg"))
            .EnsureStatus(HttpStatusCode.NotFound);

        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureOk();
        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureStatus(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Editing_a_completed_room_into_an_incomplete_state_reopens_it()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen", "Bathroom"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var rooms = await TestData.AcceptAndStartAsync(agent, inspectionId);
        await TestData.CompleteRoomAsync(agent, inspectionId, rooms[0]);
        var url = $"/api/agent/inspections/{inspectionId}/rooms/{rooms[0]}";

        // Harmless edit keeps it complete.
        var kept = (await agent.PutAsync(url, new { finalDescription = "Updated text.", defectsFound = false, agentNotes = "note" })).EnsureOk();
        Assert.Equal("Completed", kept["status"].GetValue<string>());
        // Clearing the description reopens it.
        var reopened = (await agent.PutAsync(url, new { finalDescription = "", defectsFound = false })).EnsureOk();
        Assert.Equal("InProgress", reopened["status"].GetValue<string>());
        // Adding a defect to a completed room reopens it too.
        (await agent.PutAsync(url, new { finalDescription = "Fine.", defectsFound = false })).EnsureOk();
        (await agent.PostAsync($"{url}/complete")).EnsureOk();
        var withDefect = (await agent.PostAsync($"{url}/defects", new { description = "Crack" })).EnsureOk();
        Assert.Equal("InProgress", withDefect["status"].GetValue<string>());
    }

    [Fact]
    public async Task Company_can_cancel_before_completion_but_not_after()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var cancelMe = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var cancelled = (await company.PostAsync($"/api/inspections/{cancelMe}/cancel")).EnsureOk();
        Assert.Equal("Cancelled", cancelled["status"].GetValue<string>());

        var finalizeMe = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var rooms = await TestData.AcceptAndStartAsync(agent, finalizeMe);
        await TestData.CompleteAndFinalizeAsync(agent, finalizeMe, rooms);
        (await company.PostAsync($"/api/inspections/{finalizeMe}/cancel")).EnsureStatus(HttpStatusCode.UnprocessableEntity);
    }
}

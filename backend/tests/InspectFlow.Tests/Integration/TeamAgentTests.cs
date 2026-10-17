using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// Companies create their own agents' accounts (ADR 0022). Inspections are Public to that team or Direct to one agent,
/// and the company can move an inspection between agents until it starts.
/// </summary>
[Collection(ApiCollection.Name)]
public class TeamAgentTests(TestApp app)
{
    private static bool Lists(ApiResponse res, Guid id) =>
        res.Body!.AsArray().Any(i => i!["id"]!.GetValue<string>() == id.ToString());

    private static IEnumerable<string> Actions(ApiResponse details) =>
        details["allowedActions"].AsArray().Select(a => a!.GetValue<string>());

    private async Task<(ApiClient Company, Guid PropertyId, Guid TenancyId)> CompanyWithPropertyAsync()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen", "Bathroom"]);
        return (company, propertyId, await TestData.CreateTenancyAsync(company, propertyId));
    }

    [Fact]
    public async Task Company_creates_a_new_agent_account_that_can_sign_in()
    {
        var company = await TestData.CompanyAsync(app);
        var email = $"team-{Guid.NewGuid():N}@test.local";
        var created = (await company.PostAsync("/api/companies/me/agents",
            new { email, fullName = "Ana Inspector", password = TestData.Password, phone = "+55 11 5555 0000" })).EnsureOk();
        Assert.Equal("Ana Inspector", created["fullName"].GetValue<string>());
        Assert.Equal("+55 11 5555 0000", created["phone"].GetValue<string>());

        var login = (await app.CreateClient().PostAsync("/api/auth/login", new { email, password = TestData.Password })).EnsureOk();
        Assert.Contains("Agent", login["user"]["roles"].AsArray().Select(r => r!.GetValue<string>()));

        var team = (await company.GetAsync("/api/companies/me/agents")).EnsureOk();
        Assert.Equal(email, Assert.Single(team.Body!.AsArray())!["email"]!.GetValue<string>());
    }

    [Fact]
    public async Task Registration_never_links_an_existing_account_and_requires_a_password()
    {
        var company = await TestData.CompanyAsync(app);
        var member = await TestData.TeamAgentAsync(app, company);
        var otherCompanyAgent = await TestData.OtherCompanyAgentAsync(app);
        var tenant = await app.CreateClient().RegisterAsync("Tenant");

        // Same answer for every existing account, so the role of an address is not revealed.
        foreach (var email in new[] { member.Email, otherCompanyAgent.Email, tenant.Email })
        {
            var res = await company.PostAsync("/api/companies/me/agents", new { email, fullName = "Taken", password = TestData.Password });
            res.EnsureStatus(HttpStatusCode.BadRequest);
            Assert.Contains("already used by an account", res.Raw, StringComparison.Ordinal);
        }
        Assert.Single((await company.GetAsync("/api/companies/me/agents")).EnsureOk().Body!.AsArray());
        (await company.PostAsync("/api/companies/me/agents", new { email = $"new-{Guid.NewGuid():N}@test.local", fullName = "No Password" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        (await member.GetAsync("/api/companies/me/agents")).EnsureStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Inspection_assigned_at_creation_goes_straight_to_the_agent()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var chosen = await TestData.TeamAgentAsync(app, company);
        var colleague = await TestData.TeamAgentAsync(app, company);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, assignedAgentId: chosen.UserId);
        var inspectionId = TestData.InspectionId(created);

        Assert.Equal("Assigned", created["status"]!.GetValue<string>());
        Assert.Equal("Direct", created["visibility"]!.GetValue<string>());
        Assert.False(Lists((await colleague.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));
        (await colleague.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        Assert.True(Lists((await chosen.GetAsync("/api/agent/inspections")).EnsureOk(), inspectionId));
        (await chosen.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureOk();

        var notifications = (await chosen.GetAsync("/api/notifications")).EnsureOk();
        Assert.Contains("team.inspection_assigned", notifications.Raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Designated_draft_is_hidden_from_the_agent_until_published()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var agent = await TestData.TeamAgentAsync(app, company);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId,
            publish: false, assignedAgentId: agent.UserId));

        (await agent.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        var published = (await company.PostAsync($"/api/inspections/{inspectionId}/publish")).EnsureOk();
        Assert.Equal("Assigned", published["status"].GetValue<string>());
        (await agent.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureOk();
    }

    [Fact]
    public async Task Assignment_requires_an_agent_of_the_company()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var outsider = await TestData.OtherCompanyAgentAsync(app);

        var res = await company.PostAsync("/api/inspections", new
        {
            propertyId, tenancyId, inspectionType = "MoveIn", publishNow = true, assignedAgentId = outsider.UserId,
        });
        res.EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Contains(outsider.UserId.ToString(), res.Raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Company_reassigns_and_releases_until_the_agent_starts()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var first = await TestData.TeamAgentAsync(app, company);
        var second = await TestData.TeamAgentAsync(app, company);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var url = $"/api/inspections/{inspectionId}/assignment";

        var assigned = (await company.PutAsync(url, new { agentId = first.UserId })).EnsureOk();
        Assert.Equal("Direct", assigned["visibility"].GetValue<string>());
        Assert.Contains("release", Actions(assigned));
        var reassigned = (await company.PutAsync(url, new { agentId = second.UserId })).EnsureOk();
        Assert.Equal(second.UserId.ToString(), reassigned["agent"]["id"].GetValue<string>());
        (await first.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);

        var released = (await company.PutAsync(url, new { agentId = (Guid?)null })).EnsureOk();
        Assert.Equal("Open", released["status"].GetValue<string>());
        Assert.Equal("Public", released["visibility"].GetValue<string>());
        Assert.True(Lists((await first.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));

        var again = await company.PutAsync(url, new { agentId = (Guid?)null });
        again.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.not_assigned", again.Code);

        await TestData.AcceptAndStartAsync(first, inspectionId);
        var late = await company.PutAsync(url, new { agentId = second.UserId });
        late.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.not_assignable", late.Code);
    }

    [Fact]
    public async Task Company_takes_back_a_public_inspection_an_agent_accepted_but_did_not_start()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var taker = await TestData.TeamAgentAsync(app, company);
        var colleague = await TestData.TeamAgentAsync(app, company);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        (await taker.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();

        var details = (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk();
        Assert.Contains("release", Actions(details));
        var released = (await company.PutAsync($"/api/inspections/{inspectionId}/assignment", new { agentId = (Guid?)null })).EnsureOk();
        Assert.Equal("Open", released["status"].GetValue<string>());
        (await taker.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        (await colleague.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
    }

    [Fact]
    public async Task Removed_agent_no_longer_sees_team_inspections_but_keeps_assigned_work()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var agent = await TestData.TeamAgentAsync(app, company);
        var assignedId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId, assignedAgentId: agent.UserId));
        var openId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var team = (await company.GetAsync("/api/companies/me/agents")).EnsureOk();
        Assert.Equal(1, team.Body!.AsArray()[0]!["activeInspections"]!.GetValue<int>());

        (await company.DeleteAsync($"/api/companies/me/agents/{agent.UserId}")).EnsureStatus(HttpStatusCode.NoContent);
        Assert.False(Lists((await agent.GetAsync("/api/agent/available")).EnsureOk(), openId));
        (await agent.GetAsync($"/api/agent/inspections/{assignedId}")).EnsureOk();
        (await company.DeleteAsync($"/api/companies/me/agents/{agent.UserId}")).EnsureStatus(HttpStatusCode.NotFound);
    }
}

using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

/// <summary>Companies register their own agents; Team inspections are open to that team or assigned to one agent.</summary>
[Collection(ApiCollection.Name)]
public class TeamAgentTests(TestApp app)
{
    private static bool Lists(ApiResponse res, Guid id) =>
        res.Body!.AsArray().Any(i => i!["id"]!.GetValue<string>() == id.ToString());

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
    public async Task Registration_rejects_duplicates_non_agents_and_new_accounts_without_password()
    {
        var company = await TestData.CompanyAsync(app);
        var agent = await TestData.TeamAgentAsync(app, company);

        var duplicate = await company.PostAsync("/api/companies/me/agents", new { email = agent.Email, fullName = "Again" });
        duplicate.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("team.agent_already_registered", duplicate.Code);

        var tenant = await app.CreateClient().RegisterAsync("Tenant");
        (await company.PostAsync("/api/companies/me/agents", new { email = tenant.Email, fullName = "Tenant" })).EnsureStatus(HttpStatusCode.BadRequest);
        (await company.PostAsync("/api/companies/me/agents", new { email = $"new-{Guid.NewGuid():N}@test.local", fullName = "No Password" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        (await agent.GetAsync("/api/companies/me/agents")).EnsureStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Team_inspection_is_listed_only_to_the_company_team()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var member = await TestData.TeamAgentAsync(app, company);
        var outsider = await app.CreateClient().RegisterAsync("Agent");
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Team"));

        var listed = (await member.GetAsync("/api/agent/available")).EnsureOk();
        Assert.Equal("Team", listed.Body!.AsArray().Single(i => i!["id"]!.GetValue<string>() == inspectionId.ToString())!["visibility"]!.GetValue<string>());
        Assert.False(Lists((await outsider.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));
        (await outsider.GetAsync($"/api/agent/available/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        (await outsider.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureStatus(HttpStatusCode.NotFound);

        var accepted = (await member.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
        Assert.Equal("Assigned", accepted["status"].GetValue<string>());
        Assert.Equal(member.UserId.ToString(), accepted["agent"]["id"].GetValue<string>());
    }

    [Fact]
    public async Task Inspection_assigned_at_creation_goes_straight_to_the_agent()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var chosen = await TestData.TeamAgentAsync(app, company);
        var colleague = await TestData.TeamAgentAsync(app, company);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Team", assignedAgentId: chosen.UserId);
        var inspectionId = TestData.InspectionId(created);

        Assert.Equal("Assigned", created["inspection"]!["status"]!.GetValue<string>());
        Assert.False(Lists((await colleague.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));
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
            visibility: "Team", publish: false, assignedAgentId: agent.UserId));

        (await agent.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        var published = (await company.PostAsync($"/api/inspections/{inspectionId}/publish")).EnsureOk();
        Assert.Equal("Assigned", published["inspection"]["status"].GetValue<string>());
        (await agent.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureOk();
    }

    [Fact]
    public async Task Assignment_requires_a_team_agent_and_a_non_public_inspection()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var outsider = await app.CreateClient().RegisterAsync("Agent");
        var member = await TestData.TeamAgentAsync(app, company);

        var notMember = await company.PostAsync("/api/inspections", new
        {
            propertyId, tenancyId, inspectionType = "MoveIn", visibility = "Team", publishNow = true, assignedAgentId = outsider.UserId,
        });
        notMember.EnsureStatus(HttpStatusCode.BadRequest);
        var publicOne = await company.PostAsync("/api/inspections", new
        {
            propertyId, tenancyId, inspectionType = "MoveIn", visibility = "Public", publishNow = true, assignedAgentId = member.UserId,
        });
        publicOne.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.public_not_assignable", publicOne.Code);
    }

    [Fact]
    public async Task Company_reassigns_and_releases_until_the_agent_starts()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var first = await TestData.TeamAgentAsync(app, company);
        var second = await TestData.TeamAgentAsync(app, company);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Team"));
        var url = $"/api/inspections/{inspectionId}/assignment";

        var assigned = (await company.PutAsync(url, new { agentId = first.UserId })).EnsureOk();
        Assert.Contains("release", assigned["allowedActions"].AsArray().Select(a => a!.GetValue<string>()));
        var reassigned = (await company.PutAsync(url, new { agentId = second.UserId })).EnsureOk();
        Assert.Equal(second.UserId.ToString(), reassigned["agent"]["id"].GetValue<string>());
        (await first.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);

        var released = (await company.PutAsync(url, new { agentId = (Guid?)null })).EnsureOk();
        Assert.Equal("Open", released["status"].GetValue<string>());
        Assert.True(Lists((await first.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));

        await TestData.AcceptAndStartAsync(first, inspectionId);
        var late = await company.PutAsync(url, new { agentId = second.UserId });
        late.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.not_assignable", late.Code);
    }

    [Fact]
    public async Task Removed_agent_no_longer_sees_team_inspections_but_keeps_assigned_work()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var agent = await TestData.TeamAgentAsync(app, company);
        var assignedId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Team", assignedAgentId: agent.UserId));
        var openId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Team"));
        var team = (await company.GetAsync("/api/companies/me/agents")).EnsureOk();
        Assert.Equal(1, team.Body!.AsArray()[0]!["activeInspections"]!.GetValue<int>());

        (await company.DeleteAsync($"/api/companies/me/agents/{agent.UserId}")).EnsureStatus(HttpStatusCode.NoContent);
        Assert.False(Lists((await agent.GetAsync("/api/agent/available")).EnsureOk(), openId));
        (await agent.GetAsync($"/api/agent/inspections/{assignedId}")).EnsureOk();
        (await company.DeleteAsync($"/api/companies/me/agents/{agent.UserId}")).EnsureStatus(HttpStatusCode.NotFound);
    }
}

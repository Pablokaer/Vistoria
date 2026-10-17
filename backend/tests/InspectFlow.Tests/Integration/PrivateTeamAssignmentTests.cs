using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// A Private inspection sent straight to one of the company's agents (ADR 0021): no link + code, only that agent
/// gets it, and earlier invitations stop working.
/// </summary>
[Collection(ApiCollection.Name)]
public class PrivateTeamAssignmentTests(TestApp app)
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
    public async Task Private_inspection_sent_to_a_team_agent_skips_the_invitation()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var chosen = await TestData.TeamAgentAsync(app, company);
        var colleague = await TestData.TeamAgentAsync(app, company);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Private", assignedAgentId: chosen.UserId);
        var inspectionId = TestData.InspectionId(created);

        Assert.Null(created["invitation"]);
        Assert.Equal("Assigned", created["inspection"]!["status"]!.GetValue<string>());
        Assert.Equal("Private", created["inspection"]!["visibility"]!.GetValue<string>());
        Assert.False(Lists((await colleague.GetAsync("/api/agent/available")).EnsureOk(), inspectionId));
        (await colleague.GetAsync($"/api/agent/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        Assert.True(Lists((await chosen.GetAsync("/api/agent/inspections")).EnsureOk(), inspectionId));
        Assert.Contains("team.inspection_assigned", (await chosen.GetAsync("/api/notifications")).EnsureOk().Raw, StringComparison.Ordinal);

        var details = (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk();
        Assert.Null(details.Body!["invitation"]);
        Assert.Contains("assign", Actions(details));
        Assert.DoesNotContain("release", Actions(details));
        (await chosen.PostAsync($"/api/agent/inspections/{inspectionId}/start")).EnsureOk();
    }

    [Fact]
    public async Task Private_assignment_requires_a_team_agent()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var outsider = await app.CreateClient().RegisterAsync("Agent");

        var res = await company.PostAsync("/api/inspections", new
        {
            propertyId, tenancyId, inspectionType = "MoveIn", visibility = "Private", publishNow = true, assignedAgentId = outsider.UserId,
        });
        res.EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Contains(outsider.UserId.ToString(), res.Raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Assigning_an_open_private_inspection_revokes_its_invitation()
    {
        var (company, propertyId, tenancyId) = await CompanyWithPropertyAsync();
        var agent = await TestData.TeamAgentAsync(app, company);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Private");
        var inspectionId = TestData.InspectionId(created);
        var link = created["invitation"]!["link"]!.GetValue<string>();
        var url = $"/api/inspections/{inspectionId}/assignment";

        var assigned = (await company.PutAsync(url, new { agentId = agent.UserId })).EnsureOk();
        Assert.Equal("Assigned", assigned["status"].GetValue<string>());
        Assert.True(assigned["invitation"]["revoked"].GetValue<bool>());

        var outsider = await app.CreateClient().RegisterAsync("Agent");
        var verify = await outsider.PostAsync($"/api/agent/invitations/{link[(link.LastIndexOf('/') + 1)..]}/verify",
            new { accessCode = created["invitation"]!["accessCode"]!.GetValue<string>() });
        verify.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("invitation.revoked", verify.Code);

        var release = await company.PutAsync(url, new { agentId = (Guid?)null });
        release.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.not_team", release.Code);
    }
}

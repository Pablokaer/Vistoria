using System.Net;
using InspectFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PrivateInvitationTests(TestApp app)
{
    private async Task<(ApiClient Company, Guid InspectionId, string Token, string Code)> PrivateInspectionAsync()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen", "Bathroom"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var created = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, visibility: "Private");
        var invitation = created["invitation"]!;
        var link = invitation["link"]!.GetValue<string>();
        Assert.Contains("/inspection/invite/", link, StringComparison.Ordinal);
        return (company, TestData.InspectionId(created), link[(link.LastIndexOf('/') + 1)..], invitation["accessCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task Private_inspection_is_hidden_from_marketplace_and_requires_link_and_code()
    {
        var (_, inspectionId, token, code) = await PrivateInspectionAsync();
        Assert.Matches("^[0-9]{6}$", code);

        var agent = await app.CreateClient().RegisterAsync("Agent");
        var available = (await agent.GetAsync("/api/agent/available")).EnsureOk();
        Assert.DoesNotContain(available.Body!.AsArray(), i => i!["id"]!.GetValue<string>() == inspectionId.ToString());
        (await agent.GetAsync($"/api/agent/available/{inspectionId}")).EnsureStatus(HttpStatusCode.NotFound);
        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureStatus(HttpStatusCode.NotFound);

        var preview = (await agent.GetAsync($"/api/agent/invitations/{token}")).EnsureOk();
        Assert.True(preview["requiresCode"].GetValue<bool>());
        Assert.Null(preview.Body!["inspection"]);

        var wrong = code == "000000" ? "111111" : "000000";
        var failed = await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = wrong });
        failed.EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Contains("4 attempt", failed.Raw, StringComparison.Ordinal);

        var ok = (await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = code })).EnsureOk();
        Assert.False(ok["requiresCode"].GetValue<bool>());
        Assert.Equal(inspectionId.ToString(), ok["inspection"]["id"].GetValue<string>());

        // A second agent cannot use the same invitation, even with the right code.
        var other = await app.CreateClient().RegisterAsync("Agent");
        (await other.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = code })).EnsureStatus(HttpStatusCode.UnprocessableEntity);
        (await other.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureStatus(HttpStatusCode.NotFound);

        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/accept")).EnsureOk();
    }

    [Fact]
    public async Task Invitation_locks_after_five_wrong_codes()
    {
        var (_, _, token, code) = await PrivateInspectionAsync();
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var wrong = code == "000000" ? "111111" : "000000";
        for (var i = 0; i < 4; i++)
            (await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = wrong })).EnsureStatus(HttpStatusCode.BadRequest);
        (await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = wrong })).EnsureStatus(HttpStatusCode.TooManyRequests);
        // Even the correct code is refused once locked.
        (await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = code })).EnsureStatus(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Codes_and_tokens_are_stored_only_as_hashes_and_regeneration_revokes_old_links()
    {
        var (company, inspectionId, token, code) = await PrivateInspectionAsync();
        await using (var db = app.CreateDbContext())
        {
            var stored = await db.InspectionInvitations.AsNoTracking().SingleAsync(x => x.InspectionId == inspectionId);
            Assert.DoesNotContain(code, stored.AccessCodeHash, StringComparison.Ordinal);
            Assert.NotEqual(token, stored.TokenHash);
            Assert.Equal(64, stored.TokenHash.Length);
            var audit = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == inspectionId.ToString()).Select(a => a.Metadata).ToListAsync();
            Assert.All(audit, m => Assert.DoesNotContain(code, m ?? string.Empty, StringComparison.Ordinal));
        }

        var regenerated = (await company.PostAsync($"/api/inspections/{inspectionId}/invitation")).EnsureOk();
        var newLink = regenerated["link"].GetValue<string>();
        Assert.DoesNotContain(token, newLink, StringComparison.Ordinal);

        var agent = await app.CreateClient().RegisterAsync("Agent");
        (await agent.PostAsync($"/api/agent/invitations/{token}/verify", new { accessCode = code })).EnsureStatus(HttpStatusCode.UnprocessableEntity);
        (await agent.GetAsync("/api/agent/invitations/not-a-real-token")).EnsureStatus(HttpStatusCode.NotFound);
    }
}

using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TenantTests(TestApp app)
{
    private async Task<(ApiClient Company, ApiClient Tenant, Guid TenancyId, Guid InspectionId)> AwaitingTenantAsync()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var tenant = await TestData.AddTenantAsync(app, company, tenancyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await TestData.TeamAgentAsync(app, company);
        await TestData.CompleteAndFinalizeAsync(agent, inspectionId, await TestData.AcceptAndStartAsync(agent, inspectionId));
        return (company, tenant, tenancyId, inspectionId);
    }

    [Fact]
    public async Task Tenant_only_sees_inspections_of_their_own_tenancy_once_sent()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var tenant = await TestData.AddTenantAsync(app, company, tenancyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));

        // Not yet finalized: invisible.
        (await tenant.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureStatus(HttpStatusCode.NotFound);
        Assert.Empty((await tenant.GetAsync("/api/tenant/dashboard")).EnsureOk()["awaitingReview"].AsArray());

        var agent = await TestData.TeamAgentAsync(app, company);
        await TestData.CompleteAndFinalizeAsync(agent, inspectionId, await TestData.AcceptAndStartAsync(agent, inspectionId));
        (await tenant.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureOk();

        // Another tenant (not a member) cannot see it through any route.
        var stranger = await TestData.TenantAsync(app);
        (await stranger.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureStatus(HttpStatusCode.NotFound);
        (await stranger.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureStatus(HttpStatusCode.NotFound);
        (await stranger.PostAsync($"/api/tenant/inspections/{inspectionId}/accept")).EnsureStatus(HttpStatusCode.NotFound);
        Assert.Empty((await stranger.GetAsync("/api/tenant/dashboard")).EnsureOk()["awaitingReview"].AsArray());
    }

    [Fact]
    public async Task Tenant_invitation_requires_matching_email_and_is_single_use()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var token = await TestData.InviteTenantAsync(company, tenancyId, $"invited-{Guid.NewGuid():N}@t.local");

        var preview = (await app.CreateClient().GetAsync($"/api/tenant/invitations/{token}")).EnsureOk();
        Assert.Contains("***", preview["invitedEmail"].GetValue<string>(), StringComparison.Ordinal);

        var wrongPerson = await TestData.TenantAsync(app);
        (await wrongPerson.PostAsync($"/api/tenant/invitations/{token}/accept")).EnsureStatus(HttpStatusCode.Forbidden);

        var agent = await TestData.TeamAgentAsync(app, company);
        (await agent.PostAsync($"/api/tenant/invitations/{token}/accept")).EnsureStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_signs_up_only_from_an_invitation_with_the_invited_email()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var email = $"invited-{Guid.NewGuid():N}@t.local";
        var token = await TestData.InviteTenantAsync(company, tenancyId, email);
        object SignUp(string address, string? invitationToken) =>
            new { email = address, password = TestData.Password, fullName = "Tom Tenant", role = "Tenant", invitationToken };

        var noToken = await app.CreateClient().PostAsync("/api/auth/register", SignUp(email, null));
        noToken.EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Contains("invitation link", noToken.Raw, StringComparison.Ordinal);
        (await app.CreateClient().PostAsync("/api/auth/register", SignUp(email, "not-a-real-token"))).EnsureStatus(HttpStatusCode.NotFound);
        (await app.CreateClient().PostAsync("/api/auth/register", SignUp($"other-{Guid.NewGuid():N}@t.local", token))).EnsureStatus(HttpStatusCode.Forbidden);

        var tenant = await app.CreateClient().RegisterAsync("Tenant", email.ToUpperInvariant(), "Tom Tenant", token);
        var tenancy = (await company.GetAsync($"/api/tenancies/{tenancyId}")).EnsureOk();
        Assert.True(tenancy["members"].AsArray().Single()!["joined"]!.GetValue<bool>());
        (await tenant.GetAsync("/api/tenant/dashboard")).EnsureOk();
        (await app.CreateClient().PostAsync("/api/auth/register", SignUp($"again-{Guid.NewGuid():N}@t.local", token))).EnsureStatus(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Existing_tenant_accepts_a_further_invitation_while_signed_in()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenant = await TestData.AddTenantAsync(app, company, await TestData.CreateTenancyAsync(company, propertyId));
        var secondTenancy = await TestData.CreateTenancyAsync(company, propertyId);
        var token = await TestData.InviteTenantAsync(company, secondTenancy, tenant.Email);

        (await tenant.PostAsync($"/api/tenant/invitations/{token}/accept")).EnsureOk();
        var tenancy = (await company.GetAsync($"/api/tenancies/{secondTenancy}")).EnsureOk();
        Assert.True(tenancy["members"].AsArray().Single()!["joined"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Dispute_requires_a_reason_and_records_the_decision()
    {
        var (company, tenant, _, inspectionId) = await AwaitingTenantAsync();
        (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/dispute", new { comment = "" })).EnsureStatus(HttpStatusCode.BadRequest);
        var disputed = (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/dispute", new { comment = "The oven was not working." })).EnsureOk();
        Assert.Equal("Disputed", disputed["inspectionStatus"].GetValue<string>());
        Assert.Equal("Disputed", disputed["responses"][0]!["decision"]!.GetValue<string>());
        Assert.Equal("The oven was not working.", disputed["responses"][0]!["comment"]!.GetValue<string>());

        (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/accept")).EnsureStatus(HttpStatusCode.UnprocessableEntity);
        (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/observations", new { text = "late" })).EnsureStatus(HttpStatusCode.UnprocessableEntity);
        var dashboard = (await tenant.GetAsync("/api/tenant/dashboard")).EnsureOk();
        Assert.Single(dashboard["disputed"].AsArray());
        Assert.Equal("Disputed", (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk()["status"].GetValue<string>());
    }

    [Fact]
    public async Task Concurrent_tenant_decisions_resolve_to_a_single_outcome()
    {
        var (company, tenant1, tenancyId, inspectionId) = await AwaitingTenantAsync();
        var tenant2 = await TestData.AddTenantAsync(app, company, tenancyId);
        var results = await Task.WhenAll(
            tenant1.PostAsync($"/api/tenant/inspections/{inspectionId}/accept"),
            tenant2.PostAsync($"/api/tenant/inspections/{inspectionId}/dispute", new { comment = "Disagree" }));
        Assert.Single(results, r => r.Status == HttpStatusCode.OK);
        Assert.Contains(results.Single(r => r.Status != HttpStatusCode.OK).Status, TestData.Rejected);
    }

    [Fact]
    public async Task Tenant_cannot_use_company_or_agent_features()
    {
        var (_, tenant, _, inspectionId) = await AwaitingTenantAsync();
        (await tenant.PostAsync("/api/properties", new { addressLine1 = "x", city = "x", postcode = "x", country = "x", propertyType = "House" })).EnsureStatus(HttpStatusCode.Forbidden);
        (await tenant.PostAsync("/api/inspections", new { })).EnsureStatus(HttpStatusCode.Forbidden);
        (await tenant.GetAsync("/api/agent/available")).EnsureStatus(HttpStatusCode.Forbidden);
        (await tenant.GetAsync($"/api/inspections/{inspectionId}")).EnsureStatus(HttpStatusCode.Forbidden);
    }
}

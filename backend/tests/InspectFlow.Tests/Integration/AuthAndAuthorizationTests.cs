using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthAndAuthorizationTests(TestApp app)
{
    [Fact]
    public async Task Register_assigns_role_and_me_returns_it()
    {
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var me = (await agent.GetAsync("/api/auth/me")).EnsureOk();
        Assert.Equal("Agent", me["roles"].AsArray().Single()!.GetValue<string>());
        Assert.Null(me.Body!["company"]);
    }

    [Fact]
    public async Task Registration_rejects_unknown_roles_and_weak_passwords()
    {
        var client = app.CreateClient();
        (await client.PostAsync("/api/auth/register", new { email = $"x{Guid.NewGuid():N}@t.local", password = TestData.Password, fullName = "X", role = "Admin" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        (await client.PostAsync("/api/auth/register", new { email = $"x{Guid.NewGuid():N}@t.local", password = "short", fullName = "X", role = "Agent" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_fails_with_wrong_password_and_refresh_rotates_with_reuse_detection()
    {
        var registered = await app.CreateClient().RegisterAsync("Tenant");
        var client = app.CreateClient();
        (await client.PostAsync("/api/auth/login", new { email = registered.Email, password = "WrongPass1" })).EnsureStatus(HttpStatusCode.Unauthorized);

        var login = (await client.PostAsync("/api/auth/login", new { email = registered.Email, password = TestData.Password })).EnsureOk();
        var refresh1 = login["refreshToken"].GetValue<string>();
        var rotated = (await client.PostAsync("/api/auth/refresh", new { refreshToken = refresh1 })).EnsureOk();
        var refresh2 = rotated["refreshToken"].GetValue<string>();
        Assert.NotEqual(refresh1, refresh2);

        // Replaying the old token fails...
        (await client.PostAsync("/api/auth/refresh", new { refreshToken = refresh1 })).EnsureStatus(HttpStatusCode.Unauthorized);
        // ...and the new one still works (replay within the grace window does not revoke the family).
        (await client.PostAsync("/api/auth/refresh", new { refreshToken = refresh2 })).EnsureOk();
    }

    [Fact]
    public async Task Web_clients_get_the_refresh_token_only_as_http_only_cookie()
    {
        var registered = await app.CreateClient().RegisterAsync("Agent");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { email = registered.Email, password = TestData.Password }),
        };
        request.Headers.Add("X-Client", "web");
        using var response = await app.Factory.CreateClient().SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"refreshToken\":null", body, StringComparison.Ordinal);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Endpoints_require_authentication_and_the_right_role()
    {
        var anonymous = app.CreateClient();
        (await anonymous.GetAsync("/api/properties")).EnsureStatus(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/agent/available")).EnsureStatus(HttpStatusCode.Unauthorized);

        var agent = await app.CreateClient().RegisterAsync("Agent");
        var tenant = await app.CreateClient().RegisterAsync("Tenant");
        var company = await TestData.CompanyAsync(app);

        (await agent.GetAsync("/api/properties")).EnsureStatus(HttpStatusCode.Forbidden);
        (await agent.PostAsync("/api/inspections", new { })).EnsureStatus(HttpStatusCode.Forbidden);
        (await tenant.GetAsync("/api/agent/available")).EnsureStatus(HttpStatusCode.Forbidden);
        (await tenant.GetAsync("/api/properties")).EnsureStatus(HttpStatusCode.Forbidden);
        (await company.GetAsync("/api/agent/available")).EnsureStatus(HttpStatusCode.Forbidden);
        (await company.GetAsync("/api/tenant/dashboard")).EnsureStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_company_cannot_see_or_change_another_companys_data()
    {
        var companyA = await TestData.CompanyAsync(app);
        var companyB = await TestData.CompanyAsync(app);
        var propertyA = await TestData.CreatePropertyAsync(companyA);
        var tenancyA = await TestData.CreateTenancyAsync(companyA, propertyA);
        var inspectionA = TestData.InspectionId(await TestData.CreateInspectionAsync(companyA, propertyA, tenancyA, publish: false));

        (await companyB.GetAsync($"/api/properties/{propertyA}")).EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.PutAsync($"/api/properties/{propertyA}", new { addressLine1 = "Hacked", city = "X", postcode = "X", country = "X", propertyType = "House" }))
            .EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.PostAsync($"/api/properties/{propertyA}/rooms", new { roomType = "Other", name = "Hidden room" })).EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.GetAsync($"/api/inspections/{inspectionA}")).EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.PostAsync($"/api/inspections/{inspectionA}/publish")).EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.GetAsync($"/api/tenancies/{tenancyA}")).EnsureStatus(HttpStatusCode.NotFound);

        // Creating an inspection or tenancy on someone else's property is rejected too.
        (await companyB.PostAsync("/api/inspections", new { propertyId = propertyA, tenancyId = tenancyA, inspectionType = "MoveIn", visibility = "Public" }))
            .EnsureStatus(HttpStatusCode.NotFound);
        (await companyB.PostAsync("/api/tenancies", new { propertyId = propertyA, startDate = "2026-01-01" })).EnsureStatus(HttpStatusCode.NotFound);

        var listB = (await companyB.GetAsync("/api/properties")).EnsureOk();
        Assert.Empty(listB.Body!.AsArray());
    }

    [Fact]
    public async Task Company_actions_require_a_workspace_first()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var res = await company.GetAsync("/api/properties");
        res.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("company.workspace_required", res.Code);
        (await company.PostAsync("/api/companies", new { name = "Acme Lettings" })).EnsureOk();
        (await company.PostAsync("/api/companies", new { name = "Second" })).EnsureStatus(HttpStatusCode.UnprocessableEntity);
    }
}

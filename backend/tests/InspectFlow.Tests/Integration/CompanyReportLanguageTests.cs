using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CompanyReportLanguageTests(TestApp app)
{
    [Fact]
    public async Task New_company_defaults_to_English_and_me_reports_it()
    {
        var company = await TestData.CompanyAsync(app);
        var me = (await company.GetAsync("/api/auth/me")).EnsureOk();
        Assert.Equal("en", me["company"]["reportLanguage"].GetValue<string>());
    }

    [Fact]
    public async Task Company_can_be_created_in_Portuguese_and_switched_back_to_English()
    {
        var client = await app.CreateClient().RegisterAsync("Company");
        await Billing.SubscribeAsync(client);
        var created = (await client.PostAsync("/api/companies", new { name = "Imobiliária Teste", reportLanguage = "pt-br" })).EnsureOk();
        Assert.Equal("pt-BR", created["reportLanguage"].GetValue<string>());

        var updated = (await client.PutAsync("/api/companies/me/report-language", new { language = "en" })).EnsureOk();
        Assert.Equal("en", updated["reportLanguage"].GetValue<string>());
    }

    [Fact]
    public async Task Unsupported_report_language_is_rejected()
    {
        var company = await TestData.CompanyAsync(app);
        var response = (await company.PutAsync("/api/companies/me/report-language", new { language = "de-DE" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Equal("validation_failed", response.Code);
    }
}

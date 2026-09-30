using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReportLanguageTests(TestApp app)
{
    private async Task<(ApiClient Company, Guid InspectionId)> FinalizedReportAsync(string reportLanguage)
    {
        var company = await TestData.CompanyAsync(app);
        (await company.PutAsync("/api/companies/me/report-language", new { language = reportLanguage })).EnsureOk();
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, await TestData.CreateTenancyAsync(company, propertyId)));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        await TestData.CompleteAndFinalizeAsync(agent, inspectionId, await TestData.AcceptAndStartAsync(agent, inspectionId));
        return (company, inspectionId);
    }

    [Fact]
    public async Task Finalized_report_records_the_company_report_language_and_keeps_it_after_a_switch()
    {
        var (company, inspectionId) = await FinalizedReportAsync("pt-BR");
        (await company.PutAsync("/api/companies/me/report-language", new { language = "en" })).EnsureOk();

        var report = (await company.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();
        Assert.Equal("pt-BR", report["snapshot"]["language"].GetValue<string>());
        Assert.Equal(2, report["snapshot"]["schemaVersion"].GetValue<int>());
    }

    [Fact]
    public async Task English_company_reports_stay_English()
    {
        var (company, inspectionId) = await FinalizedReportAsync("en");
        var report = (await company.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();
        Assert.Equal("en", report["snapshot"]["language"].GetValue<string>());
    }
}

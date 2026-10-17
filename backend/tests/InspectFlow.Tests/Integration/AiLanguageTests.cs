using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// AI drafts follow the inspector's PT | EN switch (sent as Accept-Language), not the company report language
/// (ADR 0026). The mock names the language a real provider would write in, so the tests can read it back.
/// </summary>
[Collection(ApiCollection.Name)]
public class AiLanguageTests(TestApp app)
{
    private async Task<string> RoomDraftAsync(string companyLanguage, string? agentLanguage)
    {
        var company = await TestData.CompanyAsync(app);
        (await company.PutAsync("/api/companies/me/report-language", new { language = companyLanguage })).EnsureOk();
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, null, type: "Periodic"));
        var agent = await TestData.TeamAgentAsync(app, company);
        if (agentLanguage is not null) agent.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd(agentLanguage);
        var roomId = (await TestData.AcceptAndStartAsync(agent, inspectionId))[0];

        var roomUrl = $"/api/agent/inspections/{inspectionId}/rooms/{roomId}";
        (await agent.UploadAsync($"{roomUrl}/photos", TestData.Jpeg(seed: 1), "room.jpg", "image/jpeg")).EnsureOk();
        Assert.Equal("Completed", (await agent.PostAsync($"{roomUrl}/analysis")).EnsureOk()["status"].GetValue<string>());
        return (await agent.GetAsync(roomUrl)).EnsureOk()["aiDescription"].GetValue<string>();
    }

    [Fact]
    public async Task Inspector_using_portuguese_gets_portuguese_drafts_from_an_english_company()
    {
        var draft = await RoomDraftAsync(companyLanguage: "en", agentLanguage: "pt-BR");
        Assert.Contains("writes in Brazilian Portuguese", draft, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspector_using_english_gets_english_drafts_from_a_portuguese_company()
    {
        var draft = await RoomDraftAsync(companyLanguage: "pt-BR", agentLanguage: "en");
        Assert.Contains("writes in British English", draft, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_a_language_choice_drafts_are_english()
    {
        var draft = await RoomDraftAsync(companyLanguage: "pt-BR", agentLanguage: null);
        Assert.Contains("writes in British English", draft, StringComparison.Ordinal);
    }
}

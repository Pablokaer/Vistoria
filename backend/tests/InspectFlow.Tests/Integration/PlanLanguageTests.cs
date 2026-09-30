using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PlanLanguageTests(TestApp app)
{
    [Fact]
    public async Task Plans_are_served_in_the_request_language_with_English_by_default()
    {
        var english = (await app.CreateClient().GetAsync("/api/billing/plans")).EnsureOk().Body!.AsArray();
        Assert.Equal("Professional", english[0]!["name"]!.GetValue<string>());

        var client = app.CreateClient();
        client.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
        var portuguese = (await client.GetAsync("/api/billing/plans")).EnsureOk().Body!.AsArray();
        Assert.Equal("Profissional", portuguese[0]!["name"]!.GetValue<string>());
        Assert.Equal(english[0]!["priceCents"]!.GetValue<int>(), portuguese[0]!["priceCents"]!.GetValue<int>());
        Assert.NotEqual(english[0]!["features"]![0]!.GetValue<string>(), portuguese[0]!["features"]![0]!.GetValue<string>());
    }
}

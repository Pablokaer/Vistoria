using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ErrorLanguageTests(TestApp app)
{
    private ApiClient PortugueseClient()
    {
        var client = app.CreateClient();
        client.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
        return client;
    }

    [Fact]
    public async Task Validation_errors_are_returned_in_Portuguese_for_pt_BR()
    {
        var response = (await PortugueseClient().PostAsync("/api/auth/register", new { email = "", password = "", fullName = "", role = "Agent" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        Assert.Equal("O cadastro é inválido.", response["title"].GetValue<string>());
        Assert.Equal("O e-mail é obrigatório.", response["errors"]["Email"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Identity_password_rules_are_returned_in_Portuguese()
    {
        var response = (await PortugueseClient().PostAsync("/api/auth/register",
                new { email = $"pt{Guid.NewGuid():N}@t.local", password = "short", fullName = "Ana", role = "Agent" }))
            .EnsureStatus(HttpStatusCode.BadRequest);
        var passwordErrors = response["errors"]["Password"]!.AsArray().Select(e => e!.GetValue<string>()).ToList();
        Assert.Contains(passwordErrors, e => e.StartsWith("A senha deve", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Sign_in_failure_follows_the_header_and_defaults_to_English()
    {
        var body = new { email = $"nobody{Guid.NewGuid():N}@t.local", password = "Wrong1!" };
        var portuguese = (await PortugueseClient().PostAsync("/api/auth/login", body)).EnsureStatus(HttpStatusCode.Unauthorized);
        Assert.Equal("E-mail ou senha inválidos.", portuguese["title"].GetValue<string>());

        var english = (await app.CreateClient().PostAsync("/api/auth/login", body)).EnsureStatus(HttpStatusCode.Unauthorized);
        Assert.Equal("Invalid email or password.", english["title"].GetValue<string>());
    }

    [Fact]
    public async Task Subscription_required_is_returned_in_Portuguese()
    {
        var client = await PortugueseClient().RegisterAsync("Company");
        var response = (await client.GetAsync("/api/company/dashboard")).EnsureStatus(HttpStatusCode.PaymentRequired);
        Assert.Equal("É necessária uma assinatura ativa.", response["title"].GetValue<string>());
    }
}

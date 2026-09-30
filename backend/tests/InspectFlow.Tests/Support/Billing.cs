using System.Text;
using System.Text.Json.Nodes;
using InspectFlow.Infrastructure.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace InspectFlow.Tests.Support;

/// <summary>Drives the Sandbox payment provider the way the web app does (checkout → sandbox page → pay).</summary>
public static class Billing
{
    public const string Plan = "professional";

    public sealed record StartedCheckout(Guid CheckoutId, string CheckoutUrl)
    {
        /// <summary>The sandbox URL ends with the provider session id (/checkout/sandbox/{id}).</summary>
        public string SessionId => CheckoutUrl[(CheckoutUrl.LastIndexOf('/') + 1)..];
    }

    public static async Task<StartedCheckout> StartCheckoutAsync(ApiClient client)
    {
        var res = (await client.PostAsync("/api/billing/checkout", new { planCode = Plan })).EnsureOk();
        return new StartedCheckout(Guid.Parse(res["checkoutId"].GetValue<string>()), res["checkoutUrl"].GetValue<string>());
    }

    public static Task<ApiResponse> PayAsync(ApiClient client, StartedCheckout checkout, string card = SandboxCheckoutSimulator.ApprovedCard) =>
        client.PostAsync($"/api/billing/sandbox/sessions/{checkout.SessionId}/pay", new { cardNumber = card });

    /// <summary>Full happy path: the user ends up with an Active subscription.</summary>
    public static async Task SubscribeAsync(ApiClient client)
    {
        var checkout = await StartCheckoutAsync(client);
        var paid = (await PayAsync(client, checkout)).EnsureOk();
        Assert.True(paid["approved"].GetValue<bool>());
    }

    public static async Task<JsonNode> SubscriptionAsync(ApiClient client) =>
        (await client.GetAsync("/api/billing/subscription")).EnsureOk().Body!;

    /// <summary>Posts a webhook signed by the in-process sandbox provider (what a real provider delivery looks like).</summary>
    public static Task<ApiResponse> PostSignedEventAsync(TestApp app, ApiClient client, JsonObject stripeEvent)
    {
        var payload = stripeEvent.ToJsonString();
        var signature = app.Factory.Services.GetRequiredService<SandboxPaymentProvider>().Sign(payload);
        return PostRawEventAsync(client, payload, signature);
    }

    public static Task<ApiResponse> PostRawEventAsync(ApiClient client, string payload, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        if (signature is not null) request.Headers.Add("Sandbox-Signature", signature);
        return client.SendAsync(request);
    }

    public static JsonObject StripeEvent(string type, JsonObject data, string? id = null) => new()
    {
        ["id"] = id ?? "evt_test_" + Guid.NewGuid().ToString("N"),
        ["type"] = type,
        ["data"] = new JsonObject { ["object"] = data },
    };
}

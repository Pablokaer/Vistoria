using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Common;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Billing;

/// <summary>
/// Development payment provider. Behaves like a hosted checkout: the user is sent to /checkout/sandbox/{id}
/// (a clearly labelled page with test cards), and "payment" produces a signed webhook event that goes through
/// exactly the same verification and processing path as a real provider. The signing secret is random per
/// process and never leaves it, so nothing outside this process can forge sandbox events.
/// </summary>
public sealed class SandboxPaymentProvider(IOptions<AppUrlOptions> urls, IClock clock) : IPaymentProvider
{
    public const string ProviderName = "Sandbox";
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);
    private readonly string _secret = SecureTokens.Create(32);

    public string Name => ProviderName;

    public string SignatureHeader => "Sandbox-Signature";

    public Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken ct)
    {
        var sessionId = "cs_sandbox_" + SecureTokens.Create(18);
        return Task.FromResult(new ProviderCheckout(sessionId, urls.Value.Web($"/checkout/sandbox/{sessionId}"), null));
    }

    public ProviderEvent ParseWebhook(string payload, string? signature)
    {
        WebhookSignature.Verify(payload, signature, _secret, clock.UtcNow, Tolerance);
        return StripeEventParser.Parse(payload);
    }

    /// <summary>Signs an event as the sandbox "provider" would. Example: <c>var header = sandbox.Sign(json);</c></summary>
    public string Sign(string payload) => WebhookSignature.Create(payload, _secret, clock.UtcNow);
}

/// <summary>Used when no provider is configured for this environment: checkout fails with a clear message.</summary>
public sealed class UnavailablePaymentProvider : IPaymentProvider
{
    public string Name => "Unavailable";

    public string SignatureHeader => "X-Unavailable-Signature";

    public Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken ct) =>
        throw new Shared.Errors.DomainRuleException("billing.unavailable",
            $"Payments are not configured on this server (checkout {request.CheckoutId}). Set Billing:Provider=Stripe with STRIPE_SECRET_KEY.");

    public ProviderEvent ParseWebhook(string payload, string? signature) =>
        throw new Shared.Errors.DomainRuleException("billing.unavailable", "Payments are not configured on this server; webhook rejected.");
}

using System.Net.Http.Headers;
using System.Text.Json;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Billing;

/// <summary>
/// Stripe Checkout (subscription mode) over the REST API — no SDK, to keep the dependency surface small.
/// Our checkout id is sent as client_reference_id and as Idempotency-Key, so retries never create two sessions.
/// Activation happens only from signed webhooks (checkout.session.completed, invoice.*, customer.subscription.deleted).
/// </summary>
public sealed class StripePaymentProvider(HttpClient http, IOptions<StripeOptions> options, IClock clock, ILogger<StripePaymentProvider> logger)
    : IPaymentProvider
{
    public const string ProviderName = "Stripe";
    private readonly StripeOptions _options = options.Value;

    public string Name => ProviderName;

    public string SignatureHeader => "Stripe-Signature";

    public async Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), "checkout/sessions"))
        {
            Content = new FormUrlEncodedContent(CheckoutForm(request)),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        message.Headers.Add("Idempotency-Key", request.CheckoutId.ToString());
        using var response = await http.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            // The body may echo request data; log only the status.
            logger.LogError("Stripe returned {Status} creating checkout {CheckoutId}", (int)response.StatusCode, request.CheckoutId);
            throw new DomainRuleException("billing.provider_error",
                new($"The payment provider returned {(int)response.StatusCode} for checkout {request.CheckoutId}. Please try again.",
                    $"O provedor de pagamento retornou {(int)response.StatusCode} para o pagamento {request.CheckoutId}. Tente novamente."));
        }
        return await ReadCheckoutAsync(response, ct);
    }

    public ProviderEvent ParseWebhook(string payload, string? signature)
    {
        WebhookSignature.Verify(payload, signature, _options.WebhookSecret ?? string.Empty, clock.UtcNow,
            TimeSpan.FromSeconds(_options.WebhookToleranceSeconds));
        return StripeEventParser.Parse(payload);
    }

    private static List<KeyValuePair<string, string>> CheckoutForm(ProviderCheckoutRequest request)
    {
        var priceId = request.Plan.StripePriceId;
        if (string.IsNullOrWhiteSpace(priceId))
            throw new DomainRuleException("billing.plan_not_configured",
                new($"Plan '{request.Plan.Code}' has no StripePriceId; expected Billing:Plans:<n>:StripePriceId = \"price_...\".",
                    $"O plano '{request.Plan.Code}' não tem StripePriceId; esperado Billing:Plans:<n>:StripePriceId = \"price_...\"."));
        return
        [
            new("mode", "subscription"),
            new("line_items[0][price]", priceId),
            new("line_items[0][quantity]", "1"),
            new("client_reference_id", request.CheckoutId.ToString()),
            new("customer_email", request.CustomerEmail),
            new("success_url", request.SuccessUrl),
            new("cancel_url", request.CancelUrl),
            new("metadata[checkout_id]", request.CheckoutId.ToString()),
            new("subscription_data[metadata][user_id]", request.UserId.ToString()),
        ];
    }

    private static async Task<ProviderCheckout> ReadCheckoutAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = document.RootElement;
        var id = root.GetProperty("id").GetString();
        var url = root.GetProperty("url").GetString();
        if (id is null || url is null)
            throw new DomainRuleException("billing.provider_error", new($"Stripe checkout response lacks id/url (id: '{id}', url: '{url}').",
                $"A resposta de pagamento do Stripe não tem id/url (id: '{id}', url: '{url}')."));
        DateTimeOffset? expires = root.TryGetProperty("expires_at", out var e) && e.ValueKind == JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeSeconds(e.GetInt64())
            : null;
        return new ProviderCheckout(id, url, expires);
    }
}

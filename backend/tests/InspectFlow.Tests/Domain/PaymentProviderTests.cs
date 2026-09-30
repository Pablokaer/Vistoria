using System.Net;
using System.Text;
using InspectFlow.Infrastructure.Billing;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InspectFlow.Tests.Domain;

/// <summary>Webhook signatures, Stripe event mapping and the Stripe checkout request — no network (fake handler).</summary>
public class PaymentProviderTests
{
    private const string Secret = "whsec_test_secret";
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class RecordingStripeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static StripePaymentProvider Stripe(RecordingStripeHandler handler) =>
        new(new HttpClient(handler), Options.Create(new StripeOptions { SecretKey = "sk_test_123", WebhookSecret = Secret }),
            new FixedClock(Now), NullLogger<StripePaymentProvider>.Instance);

    private static ProviderCheckoutRequest CheckoutRequest(string? priceId = "price_123") => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.NewGuid(), "owner@test.local",
        new BillingPlan { Code = "professional", StripePriceId = priceId }, "https://app/success", "https://app/cancel");

    [Fact]
    public void Signature_round_trips_and_rejects_tampering_wrong_secret_and_stale_timestamps()
    {
        const string payload = """{"id":"evt_1","type":"invoice.paid"}""";
        var header = WebhookSignature.Create(payload, Secret, Now);
        WebhookSignature.Verify(payload, header, Secret, Now.AddSeconds(10), TimeSpan.FromMinutes(5));

        Assert.Throws<ValidationException>(() => WebhookSignature.Verify(payload + " ", header, Secret, Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ValidationException>(() => WebhookSignature.Verify(payload, header, "other", Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ValidationException>(() => WebhookSignature.Verify(payload, header, Secret, Now.AddMinutes(6), TimeSpan.FromMinutes(5)));
        var missing = Assert.Throws<ValidationException>(() => WebhookSignature.Verify(payload, null, Secret, Now, TimeSpan.FromMinutes(5)));
        Assert.Contains("(missing)", missing.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_completed_maps_only_when_paid()
    {
        const string paid = """{"id":"evt_1","type":"checkout.session.completed","data":{"object":{"id":"cs_1","client_reference_id":"ref","payment_status":"paid","subscription":"sub_1","customer":"cus_1"}}}""";
        var e = StripeEventParser.Parse(paid);
        Assert.Equal(ProviderEventKind.CheckoutCompleted, e.Kind);
        Assert.Equal(("cs_1", "ref", "sub_1", "cus_1"), (e.ProviderSessionId, e.CheckoutReference, e.ProviderSubscriptionId, e.ProviderCustomerId));

        var unpaid = paid.Replace("\"paid\"", "\"unpaid\"", StringComparison.Ordinal);
        Assert.Equal(ProviderEventKind.Ignored, StripeEventParser.Parse(unpaid).Kind);
    }

    [Fact]
    public void Invoice_events_read_the_subscription_from_old_and_new_api_shapes_and_the_period()
    {
        const string legacy = """{"id":"evt_2","type":"invoice.paid","data":{"object":{"subscription":"sub_1","lines":{"data":[{"period":{"start":1790000000,"end":1792592000}}]}}}}""";
        var e = StripeEventParser.Parse(legacy);
        Assert.Equal(ProviderEventKind.PaymentSucceeded, e.Kind);
        Assert.Equal("sub_1", e.ProviderSubscriptionId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1792592000), e.Period!.End);

        const string current = """{"id":"evt_3","type":"invoice.payment_failed","data":{"object":{"parent":{"subscription_details":{"subscription":"sub_2"}}}}}""";
        var failed = StripeEventParser.Parse(current);
        Assert.Equal((ProviderEventKind.PaymentFailed, "sub_2"), (failed.Kind, failed.ProviderSubscriptionId));
    }

    [Fact]
    public void Unknown_events_are_ignored_and_malformed_payloads_rejected()
    {
        Assert.Equal(ProviderEventKind.Ignored, StripeEventParser.Parse("""{"id":"evt_4","type":"customer.created"}""").Kind);
        Assert.Equal(ProviderEventKind.SubscriptionEnded,
            StripeEventParser.Parse("""{"id":"evt_5","type":"customer.subscription.deleted","data":{"object":{"id":"sub_9"}}}""").Kind);
        Assert.Throws<ValidationException>(() => StripeEventParser.Parse("not json"));
        Assert.Throws<ValidationException>(() => StripeEventParser.Parse("""{"type":"invoice.paid"}"""));
    }

    [Fact]
    public async Task Stripe_checkout_is_a_subscription_session_with_our_reference_and_idempotency_key()
    {
        var handler = new RecordingStripeHandler(HttpStatusCode.OK, """{"id":"cs_live_1","url":"https://checkout.stripe.com/c/pay/cs_live_1","expires_at":1790003600}""");
        var checkout = await Stripe(handler).CreateCheckoutAsync(CheckoutRequest(), CancellationToken.None);

        Assert.Equal(("cs_live_1", "https://checkout.stripe.com/c/pay/cs_live_1"), (checkout.ProviderSessionId, checkout.Url));
        Assert.Equal("https://api.stripe.com/v1/checkout/sessions", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer sk_test_123", handler.Request.Headers.Authorization!.ToString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", handler.Request.Headers.GetValues("Idempotency-Key").Single());
        var form = WebUtility.UrlDecode(handler.RequestBody!);
        Assert.Contains("mode=subscription", form, StringComparison.Ordinal);
        Assert.Contains("line_items[0][price]=price_123", form, StringComparison.Ordinal);
        Assert.Contains("client_reference_id=11111111-1111-1111-1111-111111111111", form, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stripe_errors_and_missing_price_ids_fail_without_leaking_the_key()
    {
        var failing = new RecordingStripeHandler(HttpStatusCode.BadRequest, """{"error":{"message":"bad"}}""");
        var error = await Assert.ThrowsAsync<DomainRuleException>(() => Stripe(failing).CreateCheckoutAsync(CheckoutRequest(), CancellationToken.None));
        Assert.Equal("billing.provider_error", error.Code);
        Assert.DoesNotContain("sk_test", error.Message, StringComparison.Ordinal);

        var unused = new RecordingStripeHandler(HttpStatusCode.OK, "{}");
        var missing = await Assert.ThrowsAsync<DomainRuleException>(() => Stripe(unused).CreateCheckoutAsync(CheckoutRequest(priceId: null), CancellationToken.None));
        Assert.Equal("billing.plan_not_configured", missing.Code);
        Assert.Null(unused.Request);
    }

    [Fact]
    public void Stripe_webhook_requires_a_valid_signature()
    {
        var provider = Stripe(new RecordingStripeHandler(HttpStatusCode.OK, "{}"));
        const string payload = """{"id":"evt_6","type":"invoice.paid","data":{"object":{"subscription":"sub_1"}}}""";
        Assert.Equal("sub_1", provider.ParseWebhook(payload, WebhookSignature.Create(payload, Secret, Now)).ProviderSubscriptionId);
        Assert.Throws<ValidationException>(() => provider.ParseWebhook(payload, WebhookSignature.Create(payload, "forged", Now)));
    }
}

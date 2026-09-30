using InspectFlow.Modules.Billing.Domain;

namespace InspectFlow.Modules.Billing.Application;

/// <summary>
/// Thin port to a payment provider (Stripe, the development Sandbox, ...). The billing domain never sees
/// provider SDK types: checkout creation returns a URL, and webhooks are verified and translated into
/// provider-neutral <see cref="ProviderEvent"/>s. Secrets stay inside the implementation (server side only).
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Stable name stored on subscriptions and used in the webhook route (/api/billing/webhooks/{name}).</summary>
    string Name { get; }

    /// <summary>HTTP header carrying the webhook signature.</summary>
    string SignatureHeader { get; }

    /// <summary>
    /// Registers a hosted checkout at the provider. Must be idempotent per <see cref="ProviderCheckoutRequest.CheckoutId"/>.
    /// Example: <c>var checkout = await provider.CreateCheckoutAsync(request, ct); // redirect the user to checkout.Url</c>
    /// </summary>
    Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken ct);

    /// <summary>
    /// Verifies the signature and parses the event. Throws ValidationException when the signature is missing,
    /// invalid or too old — an unverified payload must never change a subscription.
    /// </summary>
    ProviderEvent ParseWebhook(string payload, string? signature);
}

public sealed record ProviderCheckoutRequest(
    Guid CheckoutId,
    Guid UserId,
    string CustomerEmail,
    BillingPlan Plan,
    string SuccessUrl,
    string CancelUrl);

public sealed record ProviderCheckout(string ProviderSessionId, string Url, DateTimeOffset? ExpiresAt);

public enum ProviderEventKind
{
    /// <summary>The first payment of a checkout succeeded.</summary>
    CheckoutCompleted,
    /// <summary>The checkout expired or its (asynchronous) payment failed.</summary>
    CheckoutFailed,
    /// <summary>A renewal invoice was paid.</summary>
    PaymentSucceeded,
    /// <summary>A renewal invoice could not be charged.</summary>
    PaymentFailed,
    /// <summary>The provider ended the subscription.</summary>
    SubscriptionEnded,
    /// <summary>An event type billing does not act on.</summary>
    Ignored,
}

/// <summary>A verified, provider-neutral webhook event.</summary>
public sealed record ProviderEvent(
    string EventId,
    string EventType,
    ProviderEventKind Kind,
    string? ProviderSessionId = null,
    string? CheckoutReference = null,
    string? ProviderSubscriptionId = null,
    string? ProviderCustomerId = null,
    SubscriptionPeriod? Period = null);

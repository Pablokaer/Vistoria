using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Billing.Domain;

public enum CheckoutSessionStatus
{
    Open,
    Completed,
    Failed,
    Expired,
}

/// <summary>
/// One attempt to pay for a subscription at the payment provider. At most one Open session per subscription
/// (unique index), so a user who abandons checkout and comes back resumes the same session instead of
/// creating duplicates. Our <see cref="Id"/> is sent to the provider as reference and idempotency key.
/// </summary>
public class CheckoutSession
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderSessionId { get; set; }
    public string? CheckoutUrl { get; set; }
    public CheckoutSessionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Example: <c>CheckoutSession.Open(sub, "Sandbox", now, TimeSpan.FromMinutes(60))</c></summary>
    public static CheckoutSession Open(Subscription subscription, string provider, DateTimeOffset now, TimeSpan lifetime) => new()
    {
        Id = Guid.NewGuid(),
        SubscriptionId = subscription.Id,
        UserId = subscription.UserId,
        PlanCode = subscription.PlanCode,
        Provider = provider,
        Status = CheckoutSessionStatus.Open,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
    };

    /// <summary>Open, not expired and already registered at the provider (has a URL to send the user to).</summary>
    public bool IsResumable(DateTimeOffset now) => Status == CheckoutSessionStatus.Open && ExpiresAt > now && CheckoutUrl is not null;

    public void AttachProviderSession(string providerSessionId, string checkoutUrl, DateTimeOffset? providerExpiresAt)
    {
        ProviderSessionId = providerSessionId;
        CheckoutUrl = checkoutUrl;
        if (providerExpiresAt is { } expires && expires < ExpiresAt) ExpiresAt = expires;
    }

    public void Complete(DateTimeOffset now) => Close(CheckoutSessionStatus.Completed, now);

    public void Fail(DateTimeOffset now) => Close(CheckoutSessionStatus.Failed, now);

    public void Expire(DateTimeOffset now) => Close(CheckoutSessionStatus.Expired, now);

    private void Close(CheckoutSessionStatus status, DateTimeOffset now)
    {
        if (Status == status) return;
        if (Status != CheckoutSessionStatus.Open)
            throw new DomainRuleException("checkout.closed", new($"Checkout session {Id} is {Status}; only an Open session can become {status}.",
                $"A sessão de pagamento {Id} está {Status}; apenas uma sessão Open pode passar para {status}."));
        Status = status;
        ClosedAt = now;
    }
}

/// <summary>
/// A payment-provider webhook event that was processed. The unique (Provider, ProviderEventId) index makes
/// processing idempotent: providers retry deliveries, and a replay must not apply the event twice.
/// </summary>
public class BillingEvent
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

using InspectFlow.Shared.Errors;

namespace InspectFlow.Modules.Billing.Domain;

/// <summary>
/// Lifecycle of a paid subscription. There is no free plan: only Active (and PastDue during the grace
/// period) grant access — see <see cref="SubscriptionAccessPolicy"/>.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Account created, checkout not confirmed by the payment provider yet.</summary>
    Pending,
    Active,
    /// <summary>A renewal payment failed; access continues during the grace period.</summary>
    PastDue,
    Cancelled,
    Expired,
}

/// <summary>
/// A user's subscription to a plan. Provider-agnostic: provider ids are opaque strings. Status changes only
/// through the transition methods, which are driven by verified payment-provider events (never by the browser).
/// Cancelled and Expired are terminal; subscribing again creates a new row.
/// </summary>
public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public DateTimeOffset? CurrentPeriodStart { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public DateTimeOffset? PastDueSince { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint Version { get; set; }

    /// <summary>
    /// Starts a subscription waiting for payment.
    /// Example: <c>var s = Subscription.CreatePending(userId, "professional", "Sandbox", clock.UtcNow);</c>
    /// </summary>
    public static Subscription CreatePending(Guid userId, string planCode, string provider, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        PlanCode = planCode,
        Provider = provider,
        Status = SubscriptionStatus.Pending,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>Statuses that still hold the user's single "current" subscription slot (unique index).</summary>
    public static readonly SubscriptionStatus[] OpenStatuses = [SubscriptionStatus.Pending, SubscriptionStatus.Active, SubscriptionStatus.PastDue];

    public bool IsOpen => OpenStatuses.Contains(Status);

    /// <summary>
    /// Payment confirmed (first payment or renewal): Pending/Active/PastDue → Active for the given period.
    /// Example: <c>sub.Activate(new SubscriptionPeriod(now, now.AddMonths(1)), "sub_123", "cus_456", now);</c>
    /// </summary>
    public void Activate(SubscriptionPeriod period, string? providerSubscriptionId, string? providerCustomerId, DateTimeOffset now)
    {
        EnsureTransition(SubscriptionStatus.Active);
        Status = SubscriptionStatus.Active;
        CurrentPeriodStart = period.Start;
        CurrentPeriodEnd = period.End;
        PastDueSince = null;
        ProviderSubscriptionId = providerSubscriptionId ?? ProviderSubscriptionId;
        ProviderCustomerId = providerCustomerId ?? ProviderCustomerId;
        UpdatedAt = now;
    }

    /// <summary>A renewal payment failed. Repeated failures keep the original PastDueSince (grace is not extended).</summary>
    public void MarkPastDue(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.PastDue) return;
        EnsureTransition(SubscriptionStatus.PastDue);
        Status = SubscriptionStatus.PastDue;
        PastDueSince = now;
        UpdatedAt = now;
    }

    /// <summary>The provider ended the subscription (cancelled by the customer or after unpaid invoices).</summary>
    public void Cancel(DateTimeOffset now) => End(SubscriptionStatus.Cancelled, now);

    /// <summary>The paid period (plus grace) is over without a renewal.</summary>
    public void Expire(DateTimeOffset now) => End(SubscriptionStatus.Expired, now);

    private void End(SubscriptionStatus terminal, DateTimeOffset now)
    {
        if (Status == terminal) return;
        EnsureTransition(terminal);
        Status = terminal;
        EndedAt = now;
        UpdatedAt = now;
    }

    private void EnsureTransition(SubscriptionStatus target)
    {
        if (SubscriptionTransitions.IsAllowed(Status, target)) return;
        throw new DomainRuleException("subscription.invalid_transition",
            $"Subscription {Id} cannot move from {Status} to {target}; allowed from {Status}: [{string.Join(", ", SubscriptionTransitions.From(Status))}].");
    }
}

/// <summary>A paid period [Start, End).</summary>
public sealed record SubscriptionPeriod(DateTimeOffset Start, DateTimeOffset End);

/// <summary>Allowed subscription status transitions. Active → Active is a renewal.</summary>
public static class SubscriptionTransitions
{
    private static readonly Dictionary<SubscriptionStatus, SubscriptionStatus[]> Allowed = new()
    {
        [SubscriptionStatus.Pending] = [SubscriptionStatus.Active, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Active] = [SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.PastDue] = [SubscriptionStatus.Active, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Cancelled] = [],
        [SubscriptionStatus.Expired] = [],
    };

    public static IReadOnlyList<SubscriptionStatus> From(SubscriptionStatus status) => Allowed[status];

    /// <summary>Example: <c>SubscriptionTransitions.IsAllowed(SubscriptionStatus.Pending, SubscriptionStatus.Active) // true</c></summary>
    public static bool IsAllowed(SubscriptionStatus from, SubscriptionStatus to) => Allowed[from].Contains(to);
}

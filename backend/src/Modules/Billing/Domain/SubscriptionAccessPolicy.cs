namespace InspectFlow.Modules.Billing.Domain;

/// <summary>
/// Result of evaluating a subscription at a point in time. <see cref="EffectiveStatus"/> is "None" when the user
/// never subscribed, and "Expired" when a stored Active/PastDue subscription ran past its grace period.
/// </summary>
public sealed record SubscriptionAccess(bool HasAccess, string EffectiveStatus, DateTimeOffset? AccessEndsAt)
{
    public const string NoSubscription = "None";

    public static readonly SubscriptionAccess None = new(false, NoSubscription, null);
}

/// <summary>
/// The single rule deciding whether a subscription grants access. Used by the API authorization handler,
/// the /me summary and checkout, so the backend and the UI can never disagree.
/// - Active: access until CurrentPeriodEnd + grace (tolerates late renewal webhooks).
/// - PastDue: access until PastDueSince + grace (the UI shows a billing warning).
/// - Pending, Cancelled, Expired: no access.
/// </summary>
public static class SubscriptionAccessPolicy
{
    /// <summary>
    /// Example: <c>SubscriptionAccessPolicy.Evaluate(sub, clock.UtcNow, TimeSpan.FromDays(7)).HasAccess</c>
    /// </summary>
    public static SubscriptionAccess Evaluate(Subscription? subscription, DateTimeOffset now, TimeSpan grace)
    {
        if (subscription is null) return SubscriptionAccess.None;
        return subscription.Status switch
        {
            SubscriptionStatus.Active => WithinDeadline(subscription, subscription.CurrentPeriodEnd, now, grace),
            SubscriptionStatus.PastDue => WithinDeadline(subscription, subscription.PastDueSince, now, grace),
            _ => new SubscriptionAccess(false, subscription.Status.ToString(), null),
        };
    }

    private static SubscriptionAccess WithinDeadline(Subscription subscription, DateTimeOffset? anchor, DateTimeOffset now, TimeSpan grace)
    {
        if (anchor is null) return new SubscriptionAccess(false, nameof(SubscriptionStatus.Expired), null);
        var endsAt = anchor.Value + grace;
        return now < endsAt
            ? new SubscriptionAccess(true, subscription.Status.ToString(), endsAt)
            : new SubscriptionAccess(false, nameof(SubscriptionStatus.Expired), endsAt);
    }
}

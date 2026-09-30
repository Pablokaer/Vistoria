namespace InspectFlow.Modules.Billing.Application;

public sealed record PlanDto(
    string Code,
    string Name,
    string Description,
    int PriceCents,
    string Currency,
    string Interval,
    IReadOnlyList<string> Features);

/// <summary>
/// What the client needs to route the user. <c>HasAccess</c> is true when no subscription is required for the
/// user's role; <c>Status</c> is the effective status ("None" when the user never subscribed).
/// </summary>
public sealed record SubscriptionSummaryDto(
    bool Required,
    bool HasAccess,
    string Status,
    string? PlanCode,
    string? PlanName,
    DateTimeOffset? CurrentPeriodEnd,
    DateTimeOffset? AccessEndsAt);

public sealed record StartCheckoutRequest(string PlanCode);

public sealed record CheckoutStartResponse(Guid CheckoutId, string CheckoutUrl, DateTimeOffset ExpiresAt);

public sealed record CheckoutStatusResponse(Guid CheckoutId, string Status, string PlanCode, SubscriptionSummaryDto Subscription);

public enum WebhookOutcome
{
    Applied,
    Duplicate,
    Ignored,
}

public sealed record WebhookResponse(string Outcome);

/// <summary>Web paths the provider sends the user back to. Success only shows status — it never activates anything.</summary>
public static class CheckoutPaths
{
    public static string Success(Guid checkoutId) => $"/subscription/success?checkout={checkoutId}";

    public const string Cancel = "/checkout?cancelled=1";
}

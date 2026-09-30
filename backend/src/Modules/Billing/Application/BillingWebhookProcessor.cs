using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Billing.Application;

/// <summary>
/// The only place where subscriptions change status. Input is a raw provider webhook: the provider verifies the
/// signature, then the event is recorded (unique provider event id → idempotent under retries) and applied in
/// the same transaction.
/// </summary>
public sealed class BillingWebhookProcessor(
    IAppDbContext db,
    IPaymentProvider provider,
    IAuditLogger audit,
    IClock clock,
    IOptions<BillingOptions> options,
    ILogger<BillingWebhookProcessor> logger)
{
    /// <summary>
    /// Example: <c>await processor.ProcessAsync("Stripe", body, request.Headers["Stripe-Signature"], ct) // Applied | Duplicate | Ignored</c>
    /// </summary>
    public async Task<WebhookOutcome> ProcessAsync(string providerName, string payload, string? signature, CancellationToken ct)
    {
        if (!string.Equals(providerName, provider.Name, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException("Payment provider", providerName);
        var providerEvent = provider.ParseWebhook(payload, signature);
        return await ApplyOnceAsync(providerEvent, ct);
    }

    private async Task<WebhookOutcome> ApplyOnceAsync(ProviderEvent e, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.BillingEvents.AnyAsync(b => b.Provider == provider.Name && b.ProviderEventId == e.EventId, ct))
            return WebhookOutcome.Duplicate;

        db.BillingEvents.Add(new BillingEvent
        {
            Id = Guid.NewGuid(), Provider = provider.Name, ProviderEventId = e.EventId, EventType = e.EventType, ReceivedAt = clock.UtcNow,
        });
        var outcome = await ApplyAsync(e, ct);
        await SaveOrReportConcurrentDeliveryAsync(e, ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Billing event {EventId} ({EventType}) from {Provider}: {Outcome}", e.EventId, e.EventType, provider.Name, outcome);
        return outcome;
    }

    private Task<WebhookOutcome> ApplyAsync(ProviderEvent e, CancellationToken ct) => e.Kind switch
    {
        ProviderEventKind.CheckoutCompleted => CompleteCheckoutAsync(e, ct),
        ProviderEventKind.CheckoutFailed => FailCheckoutAsync(e, ct),
        ProviderEventKind.PaymentSucceeded => RenewAsync(e, ct),
        ProviderEventKind.PaymentFailed => MarkPastDueAsync(e, ct),
        ProviderEventKind.SubscriptionEnded => EndAsync(e, ct),
        _ => Task.FromResult(WebhookOutcome.Ignored),
    };

    private async Task<WebhookOutcome> CompleteCheckoutAsync(ProviderEvent e, CancellationToken ct)
    {
        var session = await FindSessionAsync(e, ct);
        if (session is null) return Ignore(e, "unknown checkout session");
        var subscription = await db.Subscriptions.FirstAsync(s => s.Id == session.SubscriptionId, ct);
        // Money was taken even if the session was closed locally meanwhile (e.g. plan changed): still activate.
        if (session.Status == CheckoutSessionStatus.Open) session.Complete(clock.UtcNow);
        if (!subscription.IsOpen) return Ignore(e, $"subscription {subscription.Id} is {subscription.Status}");
        if (subscription.Status != SubscriptionStatus.Pending) return WebhookOutcome.Applied;

        var now = clock.UtcNow;
        subscription.Activate(e.Period ?? PlanFor(subscription).PeriodFrom(now), e.ProviderSubscriptionId, e.ProviderCustomerId, now);
        audit.Record(AuditActions.SubscriptionActivated, nameof(Subscription), subscription.Id, new { eventId = e.EventId }, subscription.UserId);
        return WebhookOutcome.Applied;
    }

    private async Task<WebhookOutcome> FailCheckoutAsync(ProviderEvent e, CancellationToken ct)
    {
        var session = await FindSessionAsync(e, ct);
        if (session is not { Status: CheckoutSessionStatus.Open }) return Ignore(e, "no open checkout session");
        session.Fail(clock.UtcNow);
        return WebhookOutcome.Applied;
    }

    private async Task<WebhookOutcome> RenewAsync(ProviderEvent e, CancellationToken ct)
    {
        var subscription = await FindByProviderSubscriptionAsync(e, ct);
        // The first invoice can arrive before checkout.session.completed; that event activates the subscription.
        if (subscription is not { Status: SubscriptionStatus.Active or SubscriptionStatus.PastDue }) return Ignore(e, "no active subscription");
        var now = clock.UtcNow;
        var start = subscription.CurrentPeriodEnd is { } end && end > now ? end : now;
        subscription.Activate(e.Period ?? PlanFor(subscription).PeriodFrom(start), e.ProviderSubscriptionId, e.ProviderCustomerId, now);
        audit.Record(AuditActions.SubscriptionActivated, nameof(Subscription), subscription.Id, new { eventId = e.EventId, renewal = true }, subscription.UserId);
        return WebhookOutcome.Applied;
    }

    private async Task<WebhookOutcome> MarkPastDueAsync(ProviderEvent e, CancellationToken ct)
    {
        var subscription = await FindByProviderSubscriptionAsync(e, ct);
        if (subscription is not { Status: SubscriptionStatus.Active or SubscriptionStatus.PastDue }) return Ignore(e, "no active subscription");
        subscription.MarkPastDue(clock.UtcNow);
        audit.Record(AuditActions.SubscriptionPastDue, nameof(Subscription), subscription.Id, new { eventId = e.EventId }, subscription.UserId);
        return WebhookOutcome.Applied;
    }

    private async Task<WebhookOutcome> EndAsync(ProviderEvent e, CancellationToken ct)
    {
        var subscription = await FindByProviderSubscriptionAsync(e, ct);
        if (subscription is not { IsOpen: true }) return Ignore(e, "no open subscription");
        subscription.Cancel(clock.UtcNow);
        audit.Record(AuditActions.SubscriptionCancelled, nameof(Subscription), subscription.Id, new { eventId = e.EventId }, subscription.UserId);
        return WebhookOutcome.Applied;
    }

    private Task<CheckoutSession?> FindSessionAsync(ProviderEvent e, CancellationToken ct)
    {
        var reference = Guid.TryParse(e.CheckoutReference, out var id) ? id : Guid.Empty;
        return db.CheckoutSessions.FirstOrDefaultAsync(c => c.Provider == provider.Name &&
            (c.Id == reference || (e.ProviderSessionId != null && c.ProviderSessionId == e.ProviderSessionId)), ct);
    }

    private Task<Subscription?> FindByProviderSubscriptionAsync(ProviderEvent e, CancellationToken ct) =>
        e.ProviderSubscriptionId is null
            ? Task.FromResult<Subscription?>(null)
            : db.Subscriptions.OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(s => s.Provider == provider.Name && s.ProviderSubscriptionId == e.ProviderSubscriptionId, ct);

    private BillingPlan PlanFor(Subscription subscription) =>
        options.Value.FindPlan(subscription.PlanCode)
        ?? throw new InvalidOperationException($"Subscription {subscription.Id} has plan '{subscription.PlanCode}', which is not configured under Billing:Plans.");

    private WebhookOutcome Ignore(ProviderEvent e, string reason)
    {
        logger.LogWarning("Billing event {EventId} ({EventType}) ignored: {Reason}", e.EventId, e.EventType, reason);
        return WebhookOutcome.Ignored;
    }

    /// <summary>Two deliveries of the same event racing: the loser gets 409 and the provider's retry sees "Duplicate".</summary>
    private async Task SaveOrReportConcurrentDeliveryAsync(ProviderEvent e, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException($"Billing event '{e.EventId}' is being processed concurrently; retry later. ({ex.GetType().Name})", "billing.event_in_progress");
        }
    }
}

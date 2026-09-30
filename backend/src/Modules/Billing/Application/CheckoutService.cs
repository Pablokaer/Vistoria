using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Billing.Application;

/// <summary>
/// Starts (or resumes) the checkout of the current user. Idempotent by design: one open subscription per user
/// and one open checkout session per subscription are enforced by unique indexes, so double clicks, parallel
/// tabs and "abandon checkout, sign in again later" all land on the same session. Starting a checkout never
/// grants access — only a verified provider event does (<see cref="BillingWebhookProcessor"/>).
/// </summary>
public sealed class CheckoutService(
    IAppDbContext db,
    IPaymentProvider provider,
    SubscriptionAccessService access,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IOptions<BillingOptions> options,
    IOptions<AppUrlOptions> urls)
{
    private static readonly TimeSpan InFlightWindow = TimeSpan.FromSeconds(30);
    private readonly BillingOptions _options = options.Value;

    /// <summary>Plans with their texts in the request's language. Example: <c>service.ListPlans()[0].Name</c>.</summary>
    public IReadOnlyList<PlanDto> ListPlans()
    {
        var language = CurrentLanguage.Get();
        return _options.Plans.Select(p => ToDto(p, p.TextsIn(language))).ToList();
    }

    private static PlanDto ToDto(BillingPlan plan, BillingPlanTexts texts) =>
        new(plan.Code, texts.Name, texts.Description, plan.PriceCents, plan.Currency, plan.Interval, texts.Features);

    public async Task<SubscriptionSummaryDto> GetMySubscriptionAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var roles = await db.UserRoles.AsNoTracking().Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name!).ToListAsync(ct);
        return await access.GetSummaryAsync(userId, roles, ct);
    }

    /// <summary>
    /// Returns where to send the user to pay.
    /// Example: <c>var start = await checkout.StartAsync(new StartCheckoutRequest("professional"), ct); // redirect to start.CheckoutUrl</c>
    /// </summary>
    public async Task<CheckoutStartResponse> StartAsync(StartCheckoutRequest request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var plan = _options.RequirePlan(request.PlanCode);
        var subscription = await PrepareSubscriptionAsync(userId, plan, ct);
        var session = await ResumeOrOpenSessionAsync(subscription, plan, ct);
        return new CheckoutStartResponse(session.Id, session.CheckoutUrl!, session.ExpiresAt);
    }

    /// <summary>Read-only status for the success page, which polls until the provider event has been processed.</summary>
    public async Task<CheckoutStatusResponse> GetStatusAsync(Guid checkoutId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var session = await db.CheckoutSessions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == checkoutId && c.UserId == userId, ct)
                      ?? throw new NotFoundException(EntityNames.Checkout, checkoutId);
        return new CheckoutStatusResponse(session.Id, session.Status.ToString(), session.PlanCode, await GetMySubscriptionAsync(ct));
    }

    private async Task<Subscription> PrepareSubscriptionAsync(Guid userId, BillingPlan plan, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var current = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId && Subscription.OpenStatuses.Contains(s.Status), ct);
        if (SubscriptionAccessPolicy.Evaluate(current, now, _options.Grace).HasAccess)
            throw new ConflictException(new("Your subscription is already active.", "Sua assinatura já está ativa."), "subscription.already_active");
        if (current is { Status: SubscriptionStatus.Pending }) return ReusePending(current, plan);

        // An Active/PastDue row past its grace period no longer grants access: close it (saved first, so the
        // filtered unique index is free) before a new one starts.
        if (current is not null)
        {
            current.Expire(now);
            audit.Record(AuditActions.SubscriptionExpired, nameof(Subscription), current.Id, userId: userId);
            await db.SaveChangesAsync(ct);
        }
        var created = Subscription.CreatePending(userId, plan.Code, provider.Name, now);
        db.Subscriptions.Add(created);
        await SaveClaimingSlotAsync(ct);
        return created;
    }

    private Subscription ReusePending(Subscription pending, BillingPlan plan)
    {
        pending.PlanCode = plan.Code;
        pending.Provider = provider.Name;
        pending.UpdatedAt = clock.UtcNow;
        return pending;
    }

    private async Task<CheckoutSession> ResumeOrOpenSessionAsync(Subscription subscription, BillingPlan plan, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var open = await db.CheckoutSessions.FirstOrDefaultAsync(c => c.SubscriptionId == subscription.Id && c.Status == CheckoutSessionStatus.Open, ct);
        if (open is not null && open.IsResumable(now) && open.PlanCode == plan.Code && open.Provider == provider.Name)
        {
            await db.SaveChangesAsync(ct);
            return open;
        }
        if (open is { CheckoutUrl: null } && now - open.CreatedAt < InFlightWindow)
            throw new ConflictException(CheckoutInProgress, "checkout.in_progress");

        open?.Expire(now);
        var session = CheckoutSession.Open(subscription, provider.Name, now, TimeSpan.FromMinutes(_options.CheckoutMinutes));
        db.CheckoutSessions.Add(session);
        audit.Record(AuditActions.SubscriptionCheckoutStarted, nameof(Subscription), subscription.Id, new { plan = plan.Code, provider = provider.Name }, subscription.UserId);
        await SaveClaimingSlotAsync(ct);
        await RegisterAtProviderAsync(session, plan, ct);
        return session;
    }

    private async Task RegisterAtProviderAsync(CheckoutSession session, BillingPlan plan, CancellationToken ct)
    {
        var email = await db.Users.Where(u => u.Id == session.UserId).Select(u => u.Email!).FirstAsync(ct);
        var app = urls.Value;
        var request = new ProviderCheckoutRequest(session.Id, session.UserId, email, plan,
            app.Web(CheckoutPaths.Success(session.Id)), app.Web(CheckoutPaths.Cancel));
        try
        {
            var created = await provider.CreateCheckoutAsync(request, ct);
            session.AttachProviderSession(created.ProviderSessionId, created.Url, created.ExpiresAt);
        }
        catch
        {
            session.Fail(clock.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A unique-index violation means a parallel request claimed the slot first; the client retries and resumes it.</summary>
    private async Task SaveClaimingSlotAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e is not DbUpdateConcurrencyException)
        {
            throw new ConflictException(CheckoutInProgress, "checkout.in_progress");
        }
    }

    private static readonly LocalizedText CheckoutInProgress =
        new("Your checkout is being prepared. Please try again in a moment.", "Seu pagamento está sendo preparado. Tente novamente em instantes.");
}

using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Tests.Domain;

public class SubscriptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Grace = TimeSpan.FromDays(7);

    private static Subscription Active(DateTimeOffset periodEnd)
    {
        var s = Subscription.CreatePending(Guid.NewGuid(), "professional", "Sandbox", Now.AddMonths(-1));
        s.Activate(new SubscriptionPeriod(periodEnd.AddMonths(-1), periodEnd), "sub_1", "cus_1", Now.AddMonths(-1));
        return s;
    }

    [Fact]
    public void No_subscription_and_pending_grant_no_access()
    {
        Assert.Equal(SubscriptionAccess.None, SubscriptionAccessPolicy.Evaluate(null, Now, Grace));
        var pending = SubscriptionAccessPolicy.Evaluate(Subscription.CreatePending(Guid.NewGuid(), "p", "Sandbox", Now), Now, Grace);
        Assert.False(pending.HasAccess);
        Assert.Equal("Pending", pending.EffectiveStatus);
    }

    [Fact]
    public void Active_grants_access_until_period_end_plus_grace_then_is_effectively_expired()
    {
        var current = SubscriptionAccessPolicy.Evaluate(Active(Now.AddDays(10)), Now, Grace);
        Assert.True(current.HasAccess);
        Assert.Equal(Now.AddDays(17), current.AccessEndsAt);

        Assert.True(SubscriptionAccessPolicy.Evaluate(Active(Now.AddDays(-6)), Now, Grace).HasAccess);
        var lapsed = SubscriptionAccessPolicy.Evaluate(Active(Now.AddDays(-8)), Now, Grace);
        Assert.False(lapsed.HasAccess);
        Assert.Equal("Expired", lapsed.EffectiveStatus);
    }

    [Fact]
    public void Past_due_keeps_access_during_grace_counted_from_the_first_failure()
    {
        var s = Active(Now.AddDays(20));
        s.MarkPastDue(Now.AddDays(-5));
        s.MarkPastDue(Now); // repeated failure does not extend the grace period
        Assert.Equal(Now.AddDays(-5), s.PastDueSince);
        Assert.True(SubscriptionAccessPolicy.Evaluate(s, Now, Grace).HasAccess);
        Assert.False(SubscriptionAccessPolicy.Evaluate(s, Now.AddDays(3), Grace).HasAccess);
    }

    [Fact]
    public void Renewal_payment_moves_past_due_back_to_active()
    {
        var s = Active(Now);
        s.MarkPastDue(Now);
        s.Activate(new SubscriptionPeriod(Now, Now.AddMonths(1)), null, null, Now);
        Assert.Equal(SubscriptionStatus.Active, s.Status);
        Assert.Null(s.PastDueSince);
        Assert.Equal("sub_1", s.ProviderSubscriptionId);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Ended_subscriptions_are_terminal_and_grant_no_access(SubscriptionStatus terminal)
    {
        var s = Active(Now.AddDays(10));
        if (terminal == SubscriptionStatus.Cancelled) s.Cancel(Now); else s.Expire(Now);
        Assert.False(SubscriptionAccessPolicy.Evaluate(s, Now, Grace).HasAccess);
        var error = Assert.Throws<DomainRuleException>(() => s.Activate(new SubscriptionPeriod(Now, Now.AddMonths(1)), null, null, Now));
        Assert.Equal("subscription.invalid_transition", error.Code);
        Assert.Contains(terminal.ToString(), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pending_cannot_become_past_due()
    {
        var s = Subscription.CreatePending(Guid.NewGuid(), "p", "Sandbox", Now);
        Assert.Throws<DomainRuleException>(() => s.MarkPastDue(Now));
    }

    [Fact]
    public void Checkout_session_is_resumable_only_while_open_unexpired_and_registered()
    {
        var sub = Subscription.CreatePending(Guid.NewGuid(), "p", "Sandbox", Now);
        var session = CheckoutSession.Open(sub, "Sandbox", Now, TimeSpan.FromMinutes(60));
        Assert.False(session.IsResumable(Now)); // not registered at the provider yet
        session.AttachProviderSession("cs_1", "https://pay/cs_1", Now.AddMinutes(30));
        Assert.True(session.IsResumable(Now));
        Assert.Equal(Now.AddMinutes(30), session.ExpiresAt); // the provider's shorter expiry wins
        Assert.False(session.IsResumable(Now.AddMinutes(31)));

        session.Complete(Now);
        Assert.False(session.IsResumable(Now));
        Assert.Throws<DomainRuleException>(() => session.Fail(Now));
    }

    [Fact]
    public void Plan_period_follows_the_interval_and_rejects_unknown_intervals()
    {
        Assert.Equal(Now.AddMonths(1), new BillingPlan { Code = "m", Interval = "Month" }.PeriodFrom(Now).End);
        Assert.Equal(Now.AddYears(1), new BillingPlan { Code = "y", Interval = "Year" }.PeriodFrom(Now).End);
        var error = Assert.Throws<InvalidOperationException>(() => new BillingPlan { Code = "w", Interval = "Week" }.PeriodFrom(Now));
        Assert.Contains("'Week'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_plan_code_is_a_validation_error_listing_the_known_plans()
    {
        var options = new BillingOptions { Plans = [new BillingPlan { Code = "professional" }] };
        Assert.Equal("professional", options.RequirePlan("PROFESSIONAL").Code);
        var error = Assert.Throws<ValidationException>(() => options.RequirePlan("gold"));
        Assert.Contains("'gold'", error.Message, StringComparison.Ordinal);
        Assert.Contains("professional", error.Message, StringComparison.Ordinal);
    }
}

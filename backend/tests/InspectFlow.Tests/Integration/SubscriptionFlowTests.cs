using System.Net;
using System.Text.Json.Nodes;
using InspectFlow.Infrastructure.Billing;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// No free plan: a Company account needs an Active subscription, confirmed only by a verified provider event,
/// before any Company API works. Authorization is enforced by the API (402 subscription.required).
/// </summary>
[Collection(ApiCollection.Name)]
public class SubscriptionFlowTests(TestApp app)
{
    private static async Task AssertBlockedAsync(ApiClient client)
    {
        var res = await client.PostAsync("/api/companies", new { name = "Blocked Lettings" });
        res.EnsureStatus(HttpStatusCode.PaymentRequired);
        Assert.Equal("subscription.required", res.Code);
        (await client.GetAsync("/api/company/dashboard")).EnsureStatus(HttpStatusCode.PaymentRequired);
    }

    private async Task<Subscription> StoredSubscriptionAsync(Guid userId)
    {
        await using var db = app.CreateDbContext();
        return await db.Subscriptions.AsNoTracking().Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).FirstAsync();
    }

    [Fact]
    public async Task Visitors_can_list_the_single_paid_plan_without_signing_in()
    {
        var plans = (await app.CreateClient().GetAsync("/api/billing/plans")).EnsureOk().Body!.AsArray();
        var plan = Assert.Single(plans)!;
        Assert.Equal(Billing.Plan, plan["code"]!.GetValue<string>());
        Assert.True(plan["priceCents"]!.GetValue<int>() > 0);
        Assert.Equal("Month", plan["interval"]!.GetValue<string>());
        Assert.NotEmpty(plan["features"]!.AsArray());
    }

    [Fact]
    public async Task New_company_account_has_no_access_until_it_subscribes()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var me = (await company.GetAsync("/api/auth/me")).EnsureOk();
        Assert.True(me["subscription"]["required"]!.GetValue<bool>());
        Assert.False(me["subscription"]["hasAccess"]!.GetValue<bool>());
        Assert.Equal("None", me["subscription"]["status"]!.GetValue<string>());
        await AssertBlockedAsync(company);
    }

    [Fact]
    public async Task Agents_and_tenants_do_not_need_a_subscription()
    {
        var agent = await app.CreateClient().RegisterAsync("Agent");
        (await agent.GetAsync("/api/agent/dashboard")).EnsureOk();
        var tenant = await app.CreateClient().RegisterAsync("Tenant");
        (await tenant.GetAsync("/api/tenant/dashboard")).EnsureOk();
        Assert.True((await Billing.SubscriptionAsync(agent))["hasAccess"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Paying_in_the_sandbox_activates_the_subscription_and_unlocks_the_company_api()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var checkout = await Billing.StartCheckoutAsync(company);
        var session = (await company.GetAsync($"/api/billing/sandbox/sessions/{checkout.SessionId}")).EnsureOk();
        Assert.Equal("Open", session["status"].GetValue<string>());

        var paid = (await Billing.PayAsync(company, checkout)).EnsureOk();
        Assert.EndsWith($"/subscription/success?checkout={checkout.CheckoutId}", paid["redirectUrl"].GetValue<string>(), StringComparison.Ordinal);
        var status = (await company.GetAsync($"/api/billing/checkout/{checkout.CheckoutId}")).EnsureOk();
        Assert.Equal("Completed", status["status"].GetValue<string>());
        Assert.Equal("Active", status["subscription"]["status"]!.GetValue<string>());

        (await company.PostAsync("/api/companies", new { name = "Paid Lettings" })).EnsureOk();
        (await company.GetAsync("/api/company/dashboard")).EnsureOk();
        var stored = await StoredSubscriptionAsync(company.UserId);
        Assert.StartsWith("sub_sandbox_", stored.ProviderSubscriptionId, StringComparison.Ordinal);
        Assert.True(stored.CurrentPeriodEnd > DateTimeOffset.UtcNow.AddDays(27));
    }

    [Fact]
    public async Task Abandoned_checkout_is_resumed_after_signing_in_again_without_a_new_account()
    {
        var registered = await app.CreateClient().RegisterAsync("Company");
        var first = await Billing.StartCheckoutAsync(registered);

        // Later, in a new browser session.
        var returning = app.CreateClient();
        var login = (await returning.PostAsync("/api/auth/login", new { email = registered.Email, password = TestData.Password })).EnsureOk();
        returning.UseToken(login["accessToken"].GetValue<string>());
        Assert.Equal("Pending", login["user"]["subscription"]!["status"]!.GetValue<string>());
        await AssertBlockedAsync(returning);

        var resumed = await Billing.StartCheckoutAsync(returning);
        Assert.Equal(first, resumed);
        (await Billing.PayAsync(returning, resumed)).EnsureOk();
        (await returning.PostAsync("/api/companies", new { name = "Returning Lettings" })).EnsureOk();

        await using var db = app.CreateDbContext();
        Assert.Equal(1, await db.Subscriptions.CountAsync(s => s.UserId == registered.UserId));
    }

    [Fact]
    public async Task Success_page_status_and_forged_webhooks_never_activate_a_subscription()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var checkout = await Billing.StartCheckoutAsync(company);
        (await company.GetAsync($"/api/billing/checkout/{checkout.CheckoutId}?status=paid&payment_status=paid")).EnsureOk();

        var forged = Billing.StripeEvent("checkout.session.completed", new JsonObject
        {
            ["id"] = checkout.SessionId, ["client_reference_id"] = checkout.CheckoutId.ToString(), ["payment_status"] = "paid",
        }).ToJsonString();
        (await Billing.PostRawEventAsync(company, forged, null)).EnsureStatus(HttpStatusCode.BadRequest);
        (await Billing.PostRawEventAsync(company, forged, WebhookSignature.Create(forged, "guessed-secret", DateTimeOffset.UtcNow)))
            .EnsureStatus(HttpStatusCode.BadRequest);

        Assert.Equal("Pending", (await Billing.SubscriptionAsync(company))["status"]!.GetValue<string>());
        await AssertBlockedAsync(company);
    }

    [Fact]
    public async Task Declined_card_leaves_the_account_blocked_and_the_checkout_open_for_another_try()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var checkout = await Billing.StartCheckoutAsync(company);
        var declined = (await Billing.PayAsync(company, checkout, SandboxCheckoutSimulator.DeclinedCard)).EnsureOk();
        Assert.False(declined["approved"].GetValue<bool>());
        await AssertBlockedAsync(company);

        (await Billing.PayAsync(company, checkout, "1234")).EnsureStatus(HttpStatusCode.BadRequest);
        (await Billing.PayAsync(company, checkout)).EnsureOk();
        (await company.GetAsync("/api/company/dashboard")).EnsureStatus(HttpStatusCode.UnprocessableEntity); // subscribed, no workspace yet
    }

    [Fact]
    public async Task Replayed_webhook_events_are_applied_once()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var checkout = await Billing.StartCheckoutAsync(company);
        var completed = Billing.StripeEvent("checkout.session.completed", new JsonObject
        {
            ["id"] = checkout.SessionId, ["client_reference_id"] = checkout.CheckoutId.ToString(), ["payment_status"] = "paid",
            ["subscription"] = "sub_replay_" + Guid.NewGuid().ToString("N"),
        });

        Assert.Equal("Applied", (await Billing.PostSignedEventAsync(app, company, completed)).EnsureOk()["outcome"].GetValue<string>());
        Assert.Equal("Duplicate", (await Billing.PostSignedEventAsync(app, company, completed)).EnsureOk()["outcome"].GetValue<string>());

        await using var db = app.CreateDbContext();
        var eventId = completed["id"]!.GetValue<string>();
        Assert.Equal(1, await db.BillingEvents.CountAsync(e => e.ProviderEventId == eventId));
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.Action == "SubscriptionActivated" && a.UserId == company.UserId));
    }

    [Fact]
    public async Task Past_due_keeps_access_during_grace_and_cancellation_blocks_it()
    {
        var company = await TestData.CompanyAsync(app);
        var providerId = (await StoredSubscriptionAsync(company.UserId)).ProviderSubscriptionId!;
        var subscription = new JsonObject { ["subscription"] = providerId };

        (await Billing.PostSignedEventAsync(app, company, Billing.StripeEvent("invoice.payment_failed", subscription))).EnsureOk();
        Assert.Equal("PastDue", (await Billing.SubscriptionAsync(company))["status"]!.GetValue<string>());
        (await company.GetAsync("/api/company/dashboard")).EnsureOk();

        (await Billing.PostSignedEventAsync(app, company, Billing.StripeEvent("customer.subscription.deleted", new JsonObject { ["id"] = providerId }))).EnsureOk();
        (await company.GetAsync("/api/company/dashboard")).EnsureStatus(HttpStatusCode.PaymentRequired);
        Assert.Equal("Cancelled", (await Billing.SubscriptionAsync(company))["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Lapsed_subscription_blocks_access_and_allows_subscribing_again()
    {
        var company = await TestData.CompanyAsync(app);
        var again = (await company.PostAsync("/api/billing/checkout", new { planCode = Billing.Plan })).EnsureStatus(HttpStatusCode.Conflict);
        Assert.Equal("subscription.already_active", again.Code);

        await using (var db = app.CreateDbContext())
        {
            var stored = await db.Subscriptions.SingleAsync(s => s.UserId == company.UserId);
            stored.CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(-30);
            await db.SaveChangesAsync();
        }
        (await company.GetAsync("/api/company/dashboard")).EnsureStatus(HttpStatusCode.PaymentRequired);
        Assert.Equal("Expired", (await Billing.SubscriptionAsync(company))["status"]!.GetValue<string>());

        await Billing.SubscribeAsync(company);
        (await company.GetAsync("/api/company/dashboard")).EnsureOk();
        await using var check = app.CreateDbContext();
        var statuses = await check.Subscriptions.Where(s => s.UserId == company.UserId).Select(s => s.Status).ToListAsync();
        Assert.Equal([SubscriptionStatus.Active, SubscriptionStatus.Expired], statuses.Order().ToList());
    }

    [Fact]
    public async Task Parallel_checkout_starts_never_create_duplicate_subscriptions_or_sessions()
    {
        var company = await app.CreateClient().RegisterAsync("Company");
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => company.PostAsync("/api/billing/checkout", new { planCode = Billing.Plan })));
        Assert.All(results, r => Assert.True(r.Status is HttpStatusCode.OK or HttpStatusCode.Conflict, r.Raw));
        Assert.Contains(results, r => r.Status == HttpStatusCode.OK);

        await using var db = app.CreateDbContext();
        Assert.Equal(1, await db.Subscriptions.CountAsync(s => s.UserId == company.UserId));
        Assert.Equal(1, await db.CheckoutSessions.CountAsync(c => c.UserId == company.UserId && c.Status == CheckoutSessionStatus.Open));
    }

    [Fact]
    public async Task Checkouts_of_other_users_are_invisible_and_unknown_plans_or_providers_are_rejected()
    {
        var owner = await app.CreateClient().RegisterAsync("Company");
        var checkout = await Billing.StartCheckoutAsync(owner);
        var other = await app.CreateClient().RegisterAsync("Company");
        (await other.GetAsync($"/api/billing/checkout/{checkout.CheckoutId}")).EnsureStatus(HttpStatusCode.NotFound);
        (await Billing.PayAsync(other, checkout)).EnsureStatus(HttpStatusCode.NotFound);

        (await other.PostAsync("/api/billing/checkout", new { planCode = "gold" })).EnsureStatus(HttpStatusCode.BadRequest);
        (await app.CreateClient().PostAsync("/api/billing/checkout", new { planCode = Billing.Plan })).EnsureStatus(HttpStatusCode.Unauthorized);
        var stripeRoute = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Stripe") { Content = new StringContent("{}") };
        (await other.SendAsync(stripeRoute)).EnsureStatus(HttpStatusCode.NotFound);
    }
}

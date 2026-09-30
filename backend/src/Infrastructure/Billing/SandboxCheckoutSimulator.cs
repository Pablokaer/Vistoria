using System.Text.Json.Nodes;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Billing;

public sealed record SandboxSessionDto(string SessionId, string PlanName, int PriceCents, string Currency, string Interval, string Status, string CustomerEmail);

public sealed record SandboxPaymentRequest(string? CardNumber);

public sealed record SandboxPaymentResult(bool Approved, string Message, string? RedirectUrl);

/// <summary>
/// The "hosted payment page" backend of the Sandbox provider (only reachable while Sandbox is the active
/// provider). An approved test card makes the sandbox emit a signed <c>checkout.session.completed</c> event into
/// <see cref="BillingWebhookProcessor"/>, the same path a real Stripe webhook takes.
/// </summary>
public sealed class SandboxCheckoutSimulator(
    IAppDbContext db,
    IPaymentProvider activeProvider,
    SandboxPaymentProvider sandbox,
    BillingWebhookProcessor webhooks,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<BillingOptions> options,
    IOptions<AppUrlOptions> urls)
{
    public const string ApprovedCard = "4242424242424242";
    public const string DeclinedCard = "4000000000000002";

    private static readonly LocalizedText CardDeclined = new("Your card was declined. Try another card.", "Seu cartão foi recusado. Tente outro cartão.");

    /// <summary>Example: <c>var session = await simulator.GetSessionAsync("cs_sandbox_abc", ct);</c></summary>
    public async Task<SandboxSessionDto> GetSessionAsync(string sessionId, CancellationToken ct)
    {
        var session = await FindOwnedSessionAsync(sessionId, ct);
        var plan = options.Value.RequirePlan(session.PlanCode);
        var email = await db.Users.Where(u => u.Id == session.UserId).Select(u => u.Email!).FirstAsync(ct);
        return new SandboxSessionDto(sessionId, plan.Name, plan.PriceCents, plan.Currency, plan.Interval, EffectiveStatus(session), email);
    }

    /// <summary>
    /// Pays with a test card: 4242 4242 4242 4242 approves, 4000 0000 0000 0002 declines (no event, like a real decline).
    /// Example: <c>await simulator.PayAsync("cs_sandbox_abc", new SandboxPaymentRequest("4242424242424242"), ct);</c>
    /// </summary>
    public async Task<SandboxPaymentResult> PayAsync(string sessionId, SandboxPaymentRequest request, CancellationToken ct)
    {
        var session = await FindOwnedSessionAsync(sessionId, ct);
        if (EffectiveStatus(session) != nameof(CheckoutSessionStatus.Open))
            throw new DomainRuleException("checkout.closed", new($"Checkout session '{sessionId}' is {EffectiveStatus(session)}; start a new checkout.",
                $"A sessão de pagamento '{sessionId}' está {EffectiveStatus(session)}; inicie um novo pagamento."));
        var card = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (card == DeclinedCard) return new SandboxPaymentResult(false, CardDeclined.ForCurrentLanguage(), null);
        if (card != ApprovedCard)
            throw new ValidationException("CardNumber", new($"Card '{card}' is not a sandbox test card; expected {ApprovedCard} (approve) or {DeclinedCard} (decline).",
                $"O cartão '{card}' não é um cartão de teste; use {ApprovedCard} (aprovar) ou {DeclinedCard} (recusar)."));

        var payload = CheckoutCompletedEvent(session);
        await webhooks.ProcessAsync(sandbox.Name, payload, sandbox.Sign(payload), ct);
        return new SandboxPaymentResult(true, "Payment approved.", urls.Value.Web(CheckoutPaths.Success(session.Id)));
    }

    private async Task<CheckoutSession> FindOwnedSessionAsync(string sessionId, CancellationToken ct)
    {
        if (activeProvider is not SandboxPaymentProvider) throw new NotFoundException(EntityNames.SandboxCheckout, sessionId);
        var userId = currentUser.RequireUserId();
        return await db.CheckoutSessions.AsNoTracking().FirstOrDefaultAsync(c =>
                   c.Provider == SandboxPaymentProvider.ProviderName && c.ProviderSessionId == sessionId && c.UserId == userId, ct)
               ?? throw new NotFoundException(EntityNames.SandboxCheckout, sessionId);
    }

    private string EffectiveStatus(CheckoutSession session) =>
        session.Status == CheckoutSessionStatus.Open && session.ExpiresAt <= clock.UtcNow ? nameof(CheckoutSessionStatus.Expired) : session.Status.ToString();

    /// <summary>A Stripe-shaped event, so the sandbox exercises the same parser as production.</summary>
    private static string CheckoutCompletedEvent(CheckoutSession session) => new JsonObject
    {
        ["id"] = "evt_sandbox_" + SecureTokens.Create(18),
        ["type"] = "checkout.session.completed",
        ["data"] = new JsonObject
        {
            ["object"] = new JsonObject
            {
                ["id"] = session.ProviderSessionId,
                ["client_reference_id"] = session.Id.ToString(),
                ["payment_status"] = "paid",
                ["subscription"] = "sub_sandbox_" + SecureTokens.Create(12),
                ["customer"] = "cus_sandbox_" + SecureTokens.Create(12),
            },
        },
    }.ToJsonString();
}

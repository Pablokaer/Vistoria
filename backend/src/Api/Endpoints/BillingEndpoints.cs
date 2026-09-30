using InspectFlow.Api.Infrastructure;
using InspectFlow.Infrastructure.Billing;
using InspectFlow.Modules.Billing.Application;

namespace InspectFlow.Api.Endpoints;

/// <summary>
/// Plans (public), the caller's subscription and checkout (authenticated, no subscription needed — this is how
/// one gets it), provider webhooks (anonymous; trust comes from the signature) and the Sandbox payment page.
/// There is intentionally no endpoint that activates a subscription from browser input.
/// </summary>
public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/billing").WithTags("Billing");

        g.MapGet("/plans", (CheckoutService s) => s.ListPlans()).RequireRateLimiting(RateLimits.Public);
        g.MapGet("/subscription", (CheckoutService s, CancellationToken ct) => s.GetMySubscriptionAsync(ct)).RequireAuthorization();
        g.MapPost("/checkout", (StartCheckoutRequest req, CheckoutService s, CancellationToken ct) => s.StartAsync(req, ct))
            .RequireAuthorization().RequireRateLimiting(RateLimits.Auth);
        g.MapGet("/checkout/{id:guid}", (Guid id, CheckoutService s, CancellationToken ct) => s.GetStatusAsync(id, ct)).RequireAuthorization();

        g.MapPost("/webhooks/{provider}", async (string provider, HttpContext http, IPaymentProvider active,
            BillingWebhookProcessor processor, CancellationToken ct) =>
        {
            using var reader = new StreamReader(http.Request.Body);
            var payload = await reader.ReadToEndAsync(ct);
            var outcome = await processor.ProcessAsync(provider, payload, http.Request.Headers[active.SignatureHeader], ct);
            return new WebhookResponse(outcome.ToString());
        });

        var sandbox = g.MapGroup("/sandbox/sessions/{sessionId}").RequireAuthorization();
        sandbox.MapGet("", (string sessionId, SandboxCheckoutSimulator s, CancellationToken ct) => s.GetSessionAsync(sessionId, ct));
        sandbox.MapPost("/pay", (string sessionId, SandboxPaymentRequest req, SandboxCheckoutSimulator s, CancellationToken ct) =>
            s.PayAsync(sessionId, req, ct)).RequireRateLimiting(RateLimits.Auth);
    }
}

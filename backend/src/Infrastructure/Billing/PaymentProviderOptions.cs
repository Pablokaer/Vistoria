namespace InspectFlow.Infrastructure.Billing;

/// <summary>Section "Billing:Stripe". Secrets only from environment / secret store; never sent to clients or logged.</summary>
public sealed class StripeOptions
{
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string BaseUrl { get; set; } = "https://api.stripe.com/v1/";
    public int WebhookToleranceSeconds { get; set; } = 300;
}

/// <summary>Section "Billing:Sandbox". The sandbox activates subscriptions without real money, so it is Development-only by default.</summary>
public sealed class SandboxOptions
{
    public bool AllowOutsideDevelopment { get; set; }
}

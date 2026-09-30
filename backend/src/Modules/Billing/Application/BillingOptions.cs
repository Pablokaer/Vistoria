using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Billing.Application;

/// <summary>
/// Billing settings (section "Billing"). Lists are empty in code on purpose: the configuration binder appends
/// to pre-filled lists, so the defaults live in appsettings.json only.
/// </summary>
public sealed class BillingOptions
{
    /// <summary>"Auto" (Stripe when a secret key is configured, otherwise Sandbox in Development), "Stripe" or "Sandbox".</summary>
    public string Provider { get; set; } = "Auto";

    /// <summary>Roles that need an active subscription for their protected API (default: Company).</summary>
    public List<string> SubscriptionRequiredRoles { get; set; } = [];

    /// <summary>Access continues this long after the paid period ends / a renewal fails (late webhooks, card updates).</summary>
    public int GraceDays { get; set; } = 7;

    /// <summary>Lifetime of a checkout session; an abandoned session is resumed until then.</summary>
    public int CheckoutMinutes { get; set; } = 60;

    public List<BillingPlan> Plans { get; set; } = [];

    public TimeSpan Grace => TimeSpan.FromDays(GraceDays);
}

/// <summary>A paid plan offered on the landing page and at checkout. New plans are added in configuration only.</summary>
public sealed class BillingPlan
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Price per interval in minor units (cents).</summary>
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "EUR";
    /// <summary>"Month" or "Year".</summary>
    public string Interval { get; set; } = "Month";
    public List<string> Features { get; set; } = [];
    /// <summary>Stripe Price id (price_...) — only needed when the Stripe provider is active.</summary>
    public string? StripePriceId { get; set; }
    /// <summary>
    /// Marketing texts per language tag (e.g. "pt-BR"). Name, Description and Features above are the English texts;
    /// a language without an entry, or an entry with an empty field, shows the English one.
    /// </summary>
    public Dictionary<string, BillingPlanTexts> Translations { get; set; } = [];

    /// <summary>
    /// The plan's texts for a language, falling back to English field by field.
    /// Example: <c>plan.TextsIn("pt-BR").Name // "Profissional"</c>
    /// </summary>
    public BillingPlanTexts TextsIn(string language)
    {
        var english = new BillingPlanTexts { Name = Name, Description = Description, Features = Features };
        var translated = Translations.FirstOrDefault(t => string.Equals(t.Key, language, StringComparison.OrdinalIgnoreCase)).Value;
        if (translated is null) return english;
        return new BillingPlanTexts
        {
            Name = string.IsNullOrWhiteSpace(translated.Name) ? Name : translated.Name,
            Description = string.IsNullOrWhiteSpace(translated.Description) ? Description : translated.Description,
            Features = translated.Features.Count == 0 ? Features : translated.Features,
        };
    }

    /// <summary>
    /// The paid period that starts at <paramref name="start"/>.
    /// Example: <c>plan.PeriodFrom(now) // [now, now + 1 month) for a monthly plan</c>
    /// </summary>
    public SubscriptionPeriod PeriodFrom(DateTimeOffset start) => Interval switch
    {
        "Month" => new SubscriptionPeriod(start, start.AddMonths(1)),
        "Year" => new SubscriptionPeriod(start, start.AddYears(1)),
        _ => throw new InvalidOperationException($"Plan '{Code}' has Interval '{Interval}'; expected \"Month\" or \"Year\"."),
    };
}

/// <summary>The customer-facing texts of a plan in one language (configuration section Plans[i].Translations.{tag}).</summary>
public sealed class BillingPlanTexts
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
}

/// <summary>Looks plans up by code.</summary>
public static class BillingPlanCatalog
{
    /// <summary>Example: <c>options.RequirePlan("professional").Name // "Professional"</c></summary>
    public static BillingPlan RequirePlan(this BillingOptions options, string? code)
    {
        var plan = options.Plans.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
        if (plan is not null) return plan;
        var known = string.Join(", ", options.Plans.Select(p => p.Code));
        throw new ValidationException("PlanCode", new($"Plan '{code}' does not exist; expected one of: {known}.", $"O plano '{code}' não existe; valores aceitos: {known}."));
    }

    /// <summary>The plan of a stored subscription, or null when the plan was removed from configuration.</summary>
    public static BillingPlan? FindPlan(this BillingOptions options, string code) =>
        options.Plans.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
}

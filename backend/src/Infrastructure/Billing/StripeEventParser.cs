using System.Text.Json;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Infrastructure.Billing;

/// <summary>
/// Translates Stripe webhook events (already signature-verified) into provider-neutral <see cref="ProviderEvent"/>s.
/// The development Sandbox emits the same dialect, so this parser is exercised in every environment.
/// </summary>
public static class StripeEventParser
{
    /// <summary>Example: <c>StripeEventParser.Parse(verifiedBody).Kind // ProviderEventKind.CheckoutCompleted</c></summary>
    public static ProviderEvent Parse(string payload)
    {
        using var document = ParseJson(payload);
        var root = document.RootElement;
        var id = RequiredString(root, "id");
        var type = RequiredString(root, "type");
        var obj = root.TryGetProperty("data", out var data) && data.TryGetProperty("object", out var o) ? o : default;
        return type switch
        {
            "checkout.session.completed" => IsPaid(obj) ? Checkout(id, type, ProviderEventKind.CheckoutCompleted, obj) : new ProviderEvent(id, type, ProviderEventKind.Ignored),
            "checkout.session.async_payment_succeeded" => Checkout(id, type, ProviderEventKind.CheckoutCompleted, obj),
            "checkout.session.async_payment_failed" or "checkout.session.expired" => Checkout(id, type, ProviderEventKind.CheckoutFailed, obj),
            "invoice.paid" => Invoice(id, type, ProviderEventKind.PaymentSucceeded, obj),
            "invoice.payment_failed" => Invoice(id, type, ProviderEventKind.PaymentFailed, obj),
            "customer.subscription.deleted" => new ProviderEvent(id, type, ProviderEventKind.SubscriptionEnded,
                ProviderSubscriptionId: OptionalString(obj, "id"), ProviderCustomerId: OptionalString(obj, "customer")),
            _ => new ProviderEvent(id, type, ProviderEventKind.Ignored),
        };
    }

    private static ProviderEvent Checkout(string id, string type, ProviderEventKind kind, JsonElement session) =>
        new(id, type, kind,
            ProviderSessionId: OptionalString(session, "id"),
            CheckoutReference: OptionalString(session, "client_reference_id"),
            ProviderSubscriptionId: OptionalString(session, "subscription"),
            ProviderCustomerId: OptionalString(session, "customer"));

    private static ProviderEvent Invoice(string id, string type, ProviderEventKind kind, JsonElement invoice) =>
        new(id, type, kind,
            ProviderSubscriptionId: InvoiceSubscriptionId(invoice),
            ProviderCustomerId: OptionalString(invoice, "customer"),
            Period: InvoicePeriod(invoice));

    /// <summary>"no_payment_required" covers 100% discounts; "unpaid" means an async method (e.g. SEPA) is still pending.</summary>
    private static bool IsPaid(JsonElement session) => OptionalString(session, "payment_status") is "paid" or "no_payment_required";

    /// <summary>Older API versions put it at invoice.subscription, newer ones at invoice.parent.subscription_details.subscription.</summary>
    private static string? InvoiceSubscriptionId(JsonElement invoice)
    {
        var legacy = OptionalString(invoice, "subscription");
        if (legacy is not null) return legacy;
        return invoice.ValueKind == JsonValueKind.Object && invoice.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty("subscription_details", out var details)
            ? OptionalString(details, "subscription")
            : null;
    }

    private static SubscriptionPeriod? InvoicePeriod(JsonElement invoice)
    {
        if (invoice.ValueKind != JsonValueKind.Object || !invoice.TryGetProperty("lines", out var lines)) return null;
        if (!lines.TryGetProperty("data", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) return null;
        if (!items[0].TryGetProperty("period", out var period)) return null;
        if (!period.TryGetProperty("start", out var start) || !period.TryGetProperty("end", out var end)) return null;
        return new SubscriptionPeriod(DateTimeOffset.FromUnixTimeSeconds(start.GetInt64()), DateTimeOffset.FromUnixTimeSeconds(end.GetInt64()));
    }

    private static JsonDocument ParseJson(string payload)
    {
        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException e)
        {
            throw new ValidationException("Payload", $"Webhook payload is not valid JSON ({e.Message}); expected a Stripe event object.");
        }
    }

    private static string RequiredString(JsonElement element, string name) =>
        OptionalString(element, name) ?? throw new ValidationException("Payload", $"Webhook event has no string '{name}'; expected a Stripe event with 'id' and 'type'.");

    private static string? OptionalString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

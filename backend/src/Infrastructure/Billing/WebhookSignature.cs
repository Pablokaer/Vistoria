using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Infrastructure.Billing;

/// <summary>
/// Stripe-style webhook signatures: header <c>t=&lt;unix seconds&gt;,v1=&lt;hex HMAC-SHA256(secret, "t.payload")&gt;</c>.
/// The timestamp is signed too, so a captured delivery cannot be replayed after <c>tolerance</c>.
/// Used by the Stripe adapter and by the development Sandbox.
/// </summary>
public static class WebhookSignature
{
    /// <summary>Example: <c>WebhookSignature.Create("{...}", secret, clock.UtcNow) // "t=1790000000,v1=ab12..."</c></summary>
    public static string Create(string payload, string secret, DateTimeOffset timestamp)
    {
        var t = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return $"t={t},v1={Compute(payload, secret, t)}";
    }

    /// <summary>Throws ValidationException (400) unless the header carries a fresh, valid v1 signature for the payload.</summary>
    public static void Verify(string payload, string? header, string secret, DateTimeOffset now, TimeSpan tolerance)
    {
        var parts = Parse(header);
        var timestamp = parts.FirstOrDefault(p => p.Key == "t").Value;
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            throw Rejected($"Signature header '{Truncate(header)}' has no numeric t=<unix seconds> part.");
        var age = now - DateTimeOffset.FromUnixTimeSeconds(seconds);
        if (age.Duration() > tolerance)
            throw Rejected($"Signature timestamp {seconds} is {age.TotalSeconds:0}s away from now; expected within {tolerance.TotalSeconds:0}s.");

        var expected = Compute(payload, secret, timestamp!);
        var valid = parts.Where(p => p.Key == "v1").Any(p => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(p.Value), Encoding.ASCII.GetBytes(expected)));
        if (!valid) throw Rejected("Signature does not match the payload (expected a valid v1=<hex HMAC-SHA256> part).");
    }

    private static List<KeyValuePair<string, string>> Parse(string? header) =>
        (header ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .Select(kv => new KeyValuePair<string, string>(kv[0], kv[1]))
            .ToList();

    private static string Compute(string payload, string secret, string timestamp)
    {
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return Convert.ToHexStringLower(mac);
    }

    private static string Truncate(string? value) => value is null ? "(missing)" : value.Length <= 40 ? value : value[..40] + "…";

    private static ValidationException Rejected(string message) => new("Signature", message);
}

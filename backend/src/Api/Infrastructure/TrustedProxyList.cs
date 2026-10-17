using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace InspectFlow.Api.Infrastructure;

/// <summary>
/// The reverse proxies whose X-Forwarded-For / X-Forwarded-Proto the API believes (ForwardedHeaders:KnownProxies, env
/// TRUSTED_PROXIES). Headers from any other sender are ignored, so a client cannot choose its own rate-limit bucket by
/// spoofing X-Forwarded-For. Never list the web container: the Next.js rewrite passes a client-supplied X-Forwarded-For
/// through unchanged (see ADR 0024).
/// </summary>
public sealed record TrustedProxyList(IReadOnlyList<IPAddress> Addresses, IReadOnlyList<IPNetwork> Networks)
{
    private const string ExpectedShape =
        "a comma-separated list of IP addresses or CIDR ranges, e.g. \"172.30.250.10\", \"2001:db8::1\" or \"10.0.0.0/8\"";

    /// <summary>
    /// Parses the configured list; throws when it is empty or an entry is neither an IP address nor a CIDR range.
    /// Example: <c>TrustedProxyList.Parse("172.30.250.10, 10.0.0.0/8")</c> → one address and one network.
    /// </summary>
    public static TrustedProxyList Parse(string? value)
    {
        var entries = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (entries.Length == 0)
            throw new InvalidOperationException(
                $"TRUST_FORWARDED_HEADERS=true requires TRUSTED_PROXIES (ForwardedHeaders:KnownProxies), {ExpectedShape}; got '{value}'.");
        var addresses = new List<IPAddress>();
        var networks = new List<IPNetwork>();
        foreach (var entry in entries) AddEntry(entry, addresses, networks);
        return new TrustedProxyList(addresses, networks);
    }

    /// <summary>
    /// Replaces the framework defaults (loopback) so that only the listed proxies are believed.
    /// Example: <c>TrustedProxyList.Parse("172.30.250.10").ApplyTo(options)</c>.
    /// </summary>
    public void ApplyTo(ForwardedHeadersOptions options)
    {
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var address in Addresses) options.KnownProxies.Add(address);
        foreach (var network in Networks) options.KnownIPNetworks.Add(network);
    }

    private static void AddEntry(string entry, List<IPAddress> addresses, List<IPNetwork> networks)
    {
        if (!entry.Contains('/') && IPAddress.TryParse(entry, out var address) && IsWrittenInFull(entry, address))
        {
            addresses.Add(address);
            return;
        }
        if (IPNetwork.TryParse(entry, out var network))
        {
            networks.Add(network);
            return;
        }
        throw new InvalidOperationException($"TRUSTED_PROXIES entry '{entry}' is not an IP address or CIDR range; expected {ExpectedShape}.");
    }

    // IPAddress.TryParse also accepts shorthand such as "172.30.250" (read as 172.30.0.250); a typo like that would
    // silently trust the wrong host, so IPv4 entries must be the canonical dotted quad.
    private static bool IsWrittenInFull(string entry, IPAddress address) =>
        address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || address.ToString() == entry;
}

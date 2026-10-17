using System.Collections.Concurrent;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Identity;

/// <summary>
/// In-process <see cref="ILoginThrottle"/>, like the HTTP rate limiter: enough for the single API instance this
/// deployment runs. Each key keeps a fixed window that starts at its first failure; old windows are pruned when the
/// table grows, so a spray of made-up emails cannot grow memory without bound.
/// </summary>
public sealed class MemoryLoginThrottle(IOptions<LoginThrottleOptions> options, IClock clock) : ILoginThrottle
{
    private const int PruneThreshold = 10_000;

    private readonly ConcurrentDictionary<string, FailureWindow> _windows = new(StringComparer.Ordinal);

    private sealed record FailureWindow(int Failures, DateTimeOffset StartedAt);

    private TimeSpan Window => TimeSpan.FromMinutes(options.Value.WindowMinutes);

    public bool IsBlocked(string email, string? ipAddress) =>
        _windows.TryGetValue(Key(email, ipAddress), out var window) && IsCurrent(window) &&
        window.Failures >= options.Value.MaxFailuresPerDevice;

    public void RecordFailure(string email, string? ipAddress)
    {
        if (_windows.Count > PruneThreshold) PruneExpired();
        _windows.AddOrUpdate(Key(email, ipAddress), _ => new FailureWindow(1, clock.UtcNow),
            (_, current) => IsCurrent(current) ? current with { Failures = current.Failures + 1 } : new FailureWindow(1, clock.UtcNow));
    }

    public void Reset(string email, string? ipAddress) => _windows.TryRemove(Key(email, ipAddress), out _);

    private bool IsCurrent(FailureWindow window) => clock.UtcNow - window.StartedAt < Window;

    private void PruneExpired()
    {
        foreach (var (key, window) in _windows)
            if (!IsCurrent(window)) _windows.TryRemove(key, out _);
    }

    // Emails compare case-insensitively, as Identity does; an unknown IP (tests, misconfigured proxy) shares one key.
    private static string Key(string email, string? ipAddress) => $"{email.Trim().ToUpperInvariant()}|{ipAddress ?? "unknown"}";
}

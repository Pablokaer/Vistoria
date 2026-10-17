using InspectFlow.Infrastructure.Identity;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Options;

namespace InspectFlow.Tests.Domain;

/// <summary>Per account + device sign-in throttle: blocks one device after N failures without touching other devices.</summary>
public class MemoryLoginThrottleTests
{
    private const string Email = "ana@agency.test";
    private const string AttackerIp = "203.0.113.7";
    private const string OwnerIp = "198.51.100.20";

    /// <summary>A clock the test moves by hand, so window expiry needs no waiting.</summary>
    private sealed class MovableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
    }

    private static (MemoryLoginThrottle Throttle, MovableClock Clock) NewThrottle()
    {
        var clock = new MovableClock();
        var options = Options.Create(new LoginThrottleOptions { MaxFailuresPerDevice = 3, WindowMinutes = 15 });
        return (new MemoryLoginThrottle(options, clock), clock);
    }

    private static void Fail(ILoginThrottle throttle, int times, string email = Email, string ip = AttackerIp)
    {
        for (var i = 0; i < times; i++) throttle.RecordFailure(email, ip);
    }

    [Fact]
    public void Blocks_a_device_after_the_allowed_failures_only_for_that_account()
    {
        var (throttle, _) = NewThrottle();
        Fail(throttle, 2);
        Assert.False(throttle.IsBlocked(Email, AttackerIp));

        Fail(throttle, 1);
        Assert.True(throttle.IsBlocked(Email, AttackerIp));
        Assert.True(throttle.IsBlocked(Email.ToUpperInvariant(), AttackerIp)); // emails compare case-insensitively
        Assert.False(throttle.IsBlocked(Email, OwnerIp)); // the account owner on another device is not affected
        Assert.False(throttle.IsBlocked("other@agency.test", AttackerIp));
    }

    [Fact]
    public void The_window_expires_and_a_successful_sign_in_resets_the_count()
    {
        var (throttle, clock) = NewThrottle();
        Fail(throttle, 3);
        clock.UtcNow = clock.UtcNow.AddMinutes(15);
        Assert.False(throttle.IsBlocked(Email, AttackerIp));

        Fail(throttle, 2);
        throttle.Reset(Email, AttackerIp);
        Fail(throttle, 2);
        Assert.False(throttle.IsBlocked(Email, AttackerIp));
    }

    [Fact]
    public void Failures_after_an_expired_window_start_a_new_count()
    {
        var (throttle, clock) = NewThrottle();
        Fail(throttle, 2);
        clock.UtcNow = clock.UtcNow.AddMinutes(16);
        Fail(throttle, 2);
        Assert.False(throttle.IsBlocked(Email, AttackerIp));
        Fail(throttle, 1);
        Assert.True(throttle.IsBlocked(Email, AttackerIp));
    }

    [Fact]
    public void An_unknown_client_ip_shares_one_key_instead_of_throwing()
    {
        var (throttle, _) = NewThrottle();
        for (var i = 0; i < 3; i++) throttle.RecordFailure(Email, null);
        Assert.True(throttle.IsBlocked(Email, null));
    }
}

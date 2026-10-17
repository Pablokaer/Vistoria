namespace InspectFlow.Modules.Identity.Application;

/// <summary>
/// Failed sign-ins counted per account <b>and</b> client IP. A device that keeps guessing one account's password is
/// stopped long before the account-wide Identity lockout, so a stranger cannot lock someone out from one machine
/// (the lockout is a DoS on the account owner). Example: <c>if (throttle.IsBlocked(email, ip)) …</c>
/// </summary>
public interface ILoginThrottle
{
    /// <summary>True while <paramref name="email"/> has had too many failures from <paramref name="ipAddress"/> in the window.</summary>
    bool IsBlocked(string email, string? ipAddress);

    void RecordFailure(string email, string? ipAddress);

    /// <summary>Forgets the failures of this account on this device (after a successful sign-in).</summary>
    void Reset(string email, string? ipAddress);
}

/// <summary>Section "Auth:LoginThrottle".</summary>
public sealed class LoginThrottleOptions
{
    /// <summary>Failures allowed per account and device in <see cref="WindowMinutes"/>; must stay below the Identity lockout (20).</summary>
    public int MaxFailuresPerDevice { get; set; } = 5;

    public int WindowMinutes { get; set; } = 15;
}

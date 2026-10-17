using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Identity;

namespace InspectFlow.Modules.Identity.Application;

/// <summary>
/// Email + password check for sign-in, hardened against two abuses found in review: locking a stranger's account from
/// one machine (the per-device <see cref="ILoginThrottle"/> stops a device after a few failures, long before the
/// account-wide Identity lockout), and learning which addresses have accounts (unknown email and wrong password get
/// the same answer and similar hashing time; "locked" is only told to someone who knows the password).
/// Example: <c>var user = await guard.VerifyAsync(request.Email, request.Password);</c>
/// </summary>
public sealed class PasswordSignInGuard(UserManager<User> userManager, ILoginThrottle throttle, ICurrentUser currentUser)
{
    private static readonly LocalizedText InvalidCredentials = new("Invalid email or password.", "E-mail ou senha inválidos.");
    private static readonly LocalizedText AccountLocked = new(
        "Account temporarily locked after repeated failed sign-ins. Try again later.",
        "Conta bloqueada temporariamente após várias tentativas de acesso sem sucesso. Tente novamente mais tarde.");
    private static readonly LocalizedText TooManyFailures = new(
        "Too many failed sign-ins for this account from this device. Try again in a few minutes.",
        "Muitas tentativas de acesso sem sucesso a esta conta a partir deste dispositivo. Tente novamente em alguns minutos.");

    // Hashed once with the same hasher settings as real passwords, only to spend the same time on unknown emails.
    private static readonly Lazy<string> UnknownAccountHash = new(() => new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString("N")));

    /// <returns>The signed-in user; throws 429 (device throttled) or 401 (wrong credentials, or locked with the right password).</returns>
    public async Task<User> VerifyAsync(string? email, string? password)
    {
        var address = email?.Trim() ?? string.Empty;
        var ip = currentUser.IpAddress;
        if (throttle.IsBlocked(address, ip)) throw new TooManyAttemptsException(TooManyFailures);
        var user = address.Length == 0 ? null : await userManager.FindByEmailAsync(address);
        if (user is null) throw RejectUnknownAccount(address, ip, password ?? string.Empty);
        if (!await userManager.CheckPasswordAsync(user, password ?? string.Empty)) throw await RejectWrongPasswordAsync(user, address, ip);
        if (await userManager.IsLockedOutAsync(user)) throw new UnauthorizedException(AccountLocked);
        await userManager.ResetAccessFailedCountAsync(user);
        throttle.Reset(address, ip);
        return user;
    }

    private UnauthorizedException RejectUnknownAccount(string address, string? ip, string password)
    {
        userManager.PasswordHasher.VerifyHashedPassword(new User(), UnknownAccountHash.Value, password);
        throttle.RecordFailure(address, ip);
        return new UnauthorizedException(InvalidCredentials);
    }

    private async Task<UnauthorizedException> RejectWrongPasswordAsync(User user, string address, string? ip)
    {
        throttle.RecordFailure(address, ip);
        await userManager.AccessFailedAsync(user);
        return new UnauthorizedException(InvalidCredentials);
    }
}

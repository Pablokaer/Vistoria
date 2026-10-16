using InspectFlow.Modules.Common;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Tenants.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Time;
using Microsoft.AspNetCore.Identity;

namespace InspectFlow.Modules.Identity.Application;

/// <param name="Phone">Stored on the agent profile; ignored for other roles.</param>
public sealed record NewUserAccount(string Email, string Password, string FullName, string Role, string? Phone = null);

/// <summary>
/// Creates a user with one role plus that role's profile (AgentProfile / TenantProfile). Shared by self-registration
/// and by companies registering their team agents. The caller owns the transaction and <c>SaveChangesAsync</c>.
/// Example: <c>var user = await factory.CreateAsync(new("ana@agency.com", "Passw0rd!", "Ana", AppRoles.Agent));</c>
/// </summary>
public sealed class UserAccountFactory(IAppDbContext db, UserManager<User> userManager, IClock clock)
{
    public static readonly LocalizedText RegistrationInvalid = new("Registration is invalid.", "O cadastro é inválido.");

    public async Task<User> CreateAsync(NewUserAccount account)
    {
        var email = account.Email.Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = account.FullName.Trim(),
            CreatedAt = clock.UtcNow,
        };
        var result = await userManager.CreateAsync(user, account.Password);
        if (!result.Succeeded) throw ToValidation(result);
        var roleResult = await userManager.AddToRoleAsync(user, account.Role);
        if (!roleResult.Succeeded) throw ToValidation(roleResult);
        AddRoleProfile(user, account);
        return user;
    }

    private void AddRoleProfile(User user, NewUserAccount account)
    {
        if (account.Role == AppRoles.Agent)
            db.AgentProfiles.Add(new AgentProfile
            {
                UserId = user.Id, DisplayName = user.FullName, CreatedAt = user.CreatedAt,
                Phone = string.IsNullOrWhiteSpace(account.Phone) ? null : account.Phone.Trim(),
            });
        if (account.Role == AppRoles.Tenant)
            db.TenantProfiles.Add(new TenantProfile { UserId = user.Id, CreatedAt = user.CreatedAt });
    }

    private static ValidationException ToValidation(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code.Contains("Password", StringComparison.Ordinal) ? "Password"
                : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "Email" : "General")
            .ToDictionary(g => g.Key, g => g.Select(IdentityErrorTexts.Of).Distinct().ToArray());
        return new ValidationException(RegistrationInvalid, errors);
    }
}

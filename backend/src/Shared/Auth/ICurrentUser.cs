using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Shared.Auth;

/// <summary>The authenticated caller, resolved from the validated JWT (never from request bodies).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }

    Guid RequireUserId() => UserId ?? throw new ForbiddenException(new("Authentication required.", "Autenticação necessária."));
}

public static class AppRoles
{
    public const string Company = "Company";
    public const string Agent = "Agent";
    public const string Tenant = "Tenant";

    public static readonly string[] All = [Company, Agent, Tenant];

    /// <summary>Roles a user may pick when self-registering.</summary>
    public static bool IsSelfRegistrable(string role) => All.Contains(role, StringComparer.Ordinal);
}

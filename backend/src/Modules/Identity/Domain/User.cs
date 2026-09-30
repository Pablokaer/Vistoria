using Microsoft.AspNetCore.Identity;

namespace InspectFlow.Modules.Identity.Domain;

/// <summary>
/// Application user. Capabilities come from roles (UserRole) and memberships
/// (CompanyMember, TenancyMember, AgentProfile) — never from a fixed "user type" column.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
}

public class Role : IdentityRole<Guid>
{
    public Role() { }
    public Role(string name) : base(name) { }
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
}

public class UserRole : IdentityUserRole<Guid>
{
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

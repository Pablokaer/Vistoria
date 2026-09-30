namespace InspectFlow.Modules.Companies.Domain;

/// <summary>A company workspace (property manager / letting agency).</summary>
public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public ICollection<CompanyMember> Members { get; } = new List<CompanyMember>();
}

/// <summary>Links a user to a company with a company-scoped role.</summary>
public class CompanyMember
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public CompanyRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Company-scoped roles. The MVP only creates Owners, but permissions are already expressed
/// per role so CompanyAdmin/PropertyManager/Employee/Viewer can be introduced without schema changes.
/// </summary>
public enum CompanyRole
{
    Owner,
    Admin,
    PropertyManager,
    Employee,
    Viewer,
}

public enum CompanyPermission
{
    View,
    ManageProperties,
    ManageInspections,
    ManageMembers,
}

public static class CompanyRolePermissions
{
    public static bool Has(CompanyRole role, CompanyPermission permission) => permission switch
    {
        CompanyPermission.View => true,
        CompanyPermission.ManageProperties => role is CompanyRole.Owner or CompanyRole.Admin or CompanyRole.PropertyManager,
        CompanyPermission.ManageInspections => role is CompanyRole.Owner or CompanyRole.Admin or CompanyRole.PropertyManager or CompanyRole.Employee,
        CompanyPermission.ManageMembers => role is CompanyRole.Owner or CompanyRole.Admin,
        _ => false,
    };
}

namespace InspectFlow.Modules.Identity.Domain;

/// <summary>Profile created for users holding the Agent role.</summary>
public class AgentProfile
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ServiceArea { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

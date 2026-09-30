namespace InspectFlow.Modules.Tenants.Domain;

public class TenantProfile
{
    public Guid UserId { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Tenant comment on a finalized report. Stored alongside (never inside) the immutable report version.
/// <see cref="InspectionRoomId"/> is optional: null means a general observation.
/// </summary>
public class TenantObservation
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public Guid ReportVersionId { get; set; }
    public Guid? InspectionRoomId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public enum TenantDecision
{
    Accepted,
    Disputed,
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public Guid ReportVersionId { get; set; }
    public Guid UserId { get; set; }
    public TenantDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

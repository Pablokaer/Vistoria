namespace InspectFlow.Modules.Notifications.Application;

public sealed record NotificationMessage(
    string Type,
    string Subject,
    string Body,
    Guid? RecipientUserId = null,
    string? RecipientEmail = null,
    string? Link = null,
    string? TransientLink = null);
// Link: safe, persisted deep link. TransientLink: contains a secret token (invitations) — handed to a
// delivery channel (e.g. email) only, never stored or logged.

/// <summary>
/// Notification abstraction. The MVP implementation records an in-app notification (saved with the
/// caller's unit of work) and logs it; email/push delivery can be added behind this interface.
/// </summary>
public interface INotificationService
{
    void Enqueue(NotificationMessage message);
}

public static class NotificationTypes
{
    public const string TenancyInvitation = "tenancy.invitation";
    public const string InspectionAccepted = "inspection.accepted";
    public const string InspectionFinalized = "inspection.finalized";
    public const string ReportAwaitingTenant = "report.awaiting_tenant";
    public const string TenantResponded = "report.tenant_responded";
    public const string TeamAgentAdded = "team.agent_added";
    public const string TeamInspectionAvailable = "team.inspection_available";
    public const string TeamInspectionAssigned = "team.inspection_assigned";
}

namespace InspectFlow.Modules.Notifications.Domain;

/// <summary>Outbox-style record of every notification sent (in-app list + future email/push delivery).</summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

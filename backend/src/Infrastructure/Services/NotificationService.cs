using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Notifications.Domain;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Logging;

namespace InspectFlow.Infrastructure.Services;

/// <summary>
/// MVP delivery: stores an in-app notification in the same unit of work and logs it (development "mailbox").
/// <see cref="NotificationMessage.TransientLink"/> (tokenised invitation links) is never persisted or logged;
/// an email/push sender plugged in here is the only consumer that may use it.
/// </summary>
public sealed class DatabaseNotificationService(AppDbContext db, IClock clock, ILogger<DatabaseNotificationService> logger) : INotificationService
{
    public void Enqueue(NotificationMessage message)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = message.RecipientUserId,
            RecipientEmail = message.RecipientEmail,
            Type = message.Type,
            Subject = message.Subject,
            Body = message.Body,
            Link = message.Link,
            CreatedAt = clock.UtcNow,
        });
        logger.LogInformation("Notification {Type} queued for user {UserId}: {Subject}", message.Type, message.RecipientUserId, message.Subject);
    }
}

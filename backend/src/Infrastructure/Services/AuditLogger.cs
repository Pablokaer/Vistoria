using System.Text.Json;
using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Time;

namespace InspectFlow.Infrastructure.Services;

public sealed class AuditLogger(AppDbContext db, ICurrentUser currentUser, IClock clock) : IAuditLogger
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] ForbiddenKeys = ["password", "token", "code", "secret", "apikey", "api_key", "accesscode"];

    public void Record(string action, string entityType, Guid entityId, object? metadata = null, Guid? userId = null)
    {
        string? json = null;
        if (metadata is not null)
        {
            json = JsonSerializer.Serialize(metadata, Json);
            // Defence in depth: refuse metadata keys that look like secrets.
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.EnumerateObject().Any(p => ForbiddenKeys.Any(k => p.Name.Equals(k, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException($"Audit metadata for {action} contains a secret-like key.");
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Timestamp = clock.UtcNow,
            Metadata = json,
            IpAddress = currentUser.IpAddress,
        });
    }
}

namespace InspectFlow.Modules.Audit.Application;

/// <summary>Adds an AuditLog entry to the current unit of work (persisted with the business change).</summary>
public interface IAuditLogger
{
    void Record(string action, string entityType, Guid entityId, object? metadata = null, Guid? userId = null);
}

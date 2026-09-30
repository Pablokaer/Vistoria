using InspectFlow.Modules.Common;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Row-level locks on the inspection row (implemented with SELECT ... FOR SHARE / FOR UPDATE).</summary>
public interface IInspectionLock
{
    /// <summary>Shared lock: many content edits may run together, but not concurrently with finalization.</summary>
    Task LockForEditAsync(Guid inspectionId, CancellationToken ct);

    /// <summary>Exclusive lock used by finalization so no edit can slip in while the report is frozen.</summary>
    Task LockForFinalizeAsync(Guid inspectionId, CancellationToken ct);
}

/// <summary>
/// Runs an agent write inside a transaction holding a lock on the inspection row, so a content edit
/// can never be committed after (or during) finalization of the same inspection.
/// </summary>
public sealed class InspectionWriteScope(IAppDbContext db, IInspectionLock locks)
{
    public async Task<T> RunAsync<T>(Guid inspectionId, Func<Task<T>> action, CancellationToken ct, bool exclusive = false)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await Lock(inspectionId, exclusive, ct);
            return await action();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await Lock(inspectionId, exclusive, ct);
        var result = await action();
        await tx.CommitAsync(ct);
        return result;
    }

    private Task Lock(Guid id, bool exclusive, CancellationToken ct) =>
        exclusive ? locks.LockForFinalizeAsync(id, ct) : locks.LockForEditAsync(id, ct);
}

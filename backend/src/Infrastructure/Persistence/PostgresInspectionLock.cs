using InspectFlow.Modules.Inspections.Application;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Infrastructure.Persistence;

public sealed class PostgresInspectionLock(AppDbContext db) : IInspectionLock
{
    public Task LockForEditAsync(Guid inspectionId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM inspections.inspections WHERE id = {inspectionId} FOR SHARE", ct);

    public Task LockForFinalizeAsync(Guid inspectionId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM inspections.inspections WHERE id = {inspectionId} FOR UPDATE", ct);
}

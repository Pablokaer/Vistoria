using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Background maintenance: expires Open inspections whose acceptance deadline passed.</summary>
public sealed class InspectionMaintenanceService(IAppDbContext db, IAuditLogger audit, IClock clock)
{
    public async Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var overdue = await db.Inspections.Where(i => i.Status == InspectionStatus.Open && i.AcceptBy != null && i.AcceptBy <= now)
            .Take(100).ToListAsync(ct);
        foreach (var inspection in overdue)
        {
            inspection.Expire(now);
            audit.Record(AuditActions.InspectionExpired, nameof(Inspection), inspection.Id);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return 0; // Someone accepted meanwhile; next run will retry the rest.
        }
        return overdue.Count;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using InspectFlow.Modules.Common;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Modules.Reports.Application;

/// <summary>Serialization and read access to immutable report snapshots.</summary>
public sealed class ReportSnapshotStore(IAppDbContext db)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(ReportSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);

    public static ReportSnapshot Deserialize(string json) =>
        JsonSerializer.Deserialize<ReportSnapshot>(json, Json) ?? throw new InvalidOperationException("Invalid report snapshot.");

    /// <summary>Latest version snapshot of the report of <paramref name="inspectionId"/>, if finalized.</summary>
    public async Task<ReportSnapshot?> GetLatestForInspectionAsync(Guid inspectionId, CancellationToken ct)
    {
        var json = await (from r in db.InspectionReports.AsNoTracking()
                          join v in db.InspectionReportVersions.AsNoTracking() on r.Id equals v.ReportId
                          where r.InspectionId == inspectionId && v.VersionNumber == r.LatestVersionNumber
                          select v.SnapshotJson).FirstOrDefaultAsync(ct);
        return json is null ? null : Deserialize(json);
    }

    public async Task<ReportRoom?> FindRoomAsync(Guid inspectionId, Guid inspectionRoomId, CancellationToken ct)
    {
        var snapshot = await GetLatestForInspectionAsync(inspectionId, ct);
        return snapshot?.Rooms.FirstOrDefault(r => r.Id == inspectionRoomId);
    }
}

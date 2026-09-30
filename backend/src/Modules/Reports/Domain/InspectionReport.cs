namespace InspectFlow.Modules.Reports.Domain;

/// <summary>The report of a finalized inspection. Content lives in immutable <see cref="InspectionReportVersion"/>s.</summary>
public class InspectionReport
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }

    /// <summary>Human-friendly unique identifier printed on the PDF (non-sequential).</summary>
    public string ReportNumber { get; set; } = string.Empty;
    public int LatestVersionNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<InspectionReportVersion> Versions { get; } = new();

    public InspectionReportVersion AddVersion(string snapshotJson, string snapshotSha256, Guid generatedBy,
        string reason, DateTimeOffset now)
    {
        var version = new InspectionReportVersion
        {
            Id = Guid.NewGuid(),
            ReportId = Id,
            VersionNumber = LatestVersionNumber + 1,
            SnapshotJson = snapshotJson,
            SnapshotSha256 = snapshotSha256,
            GeneratedAt = now,
            GeneratedBy = generatedBy,
            Reason = reason,
        };
        LatestVersionNumber = version.VersionNumber;
        Versions.Add(version);
        return version;
    }
}

/// <summary>
/// Write-once snapshot of everything the report showed at finalization (JSON + PDF + hashes).
/// Updates/deletes are rejected by the application (SaveChanges guard) and by a database trigger.
/// </summary>
public class InspectionReportVersion
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotSha256 { get; set; } = string.Empty;
    public string? PdfStorageKey { get; set; }
    public string? PdfSha256 { get; set; }
    public long? PdfSizeBytes { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public Guid GeneratedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Revocable, expiring, unguessable link to view a report without an account.</summary>
public class ReportShareLink
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}

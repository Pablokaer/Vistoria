using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Reports.Application;

/// <summary>
/// Everything a report displays, frozen at finalization and stored as JSON in InspectionReportVersion.
/// Rendering (web page, PDF) uses ONLY this snapshot, never live tables, so historical reports are reproducible.
/// Photos are referenced by immutable storage keys.
/// </summary>
public sealed record ReportSnapshot(
    int SchemaVersion,
    Guid ReportId,
    string ReportNumber,
    int VersionNumber,
    DateTimeOffset GeneratedAt,
    ReportCompany Company,
    ReportProperty Property,
    ReportInspection Inspection,
    ReportPerson? Agent,
    IReadOnlyList<ReportPerson> Tenants,
    IReadOnlyList<ReportRoom> Rooms,
    ReportComparisonSummary? Comparison,
    string Language = SupportedLanguages.English)
{
    // v2 adds Language (the company's report language at finalization). v1 snapshots have none and read as English,
    // which is what every report was before languages existed.
    public const int CurrentSchemaVersion = 2;
}

public sealed record ReportCompany(Guid Id, string Name, string? ContactEmail, string? Phone);

public sealed record ReportProperty(Guid Id, string AddressLine1, string? AddressLine2, string City, string Postcode,
    string Country, string PropertyType)
{
    public string FullAddress => string.Join(", ",
        new[] { AddressLine1, AddressLine2, City, Postcode, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed record ReportInspection(
    Guid Id,
    string Type,
    DateOnly? ScheduledDate,
    DateTimeOffset? StartedAt,
    DateTimeOffset CompletedAt,
    Guid? TenancyId,
    string? TenancyReference,
    DateOnly? TenancyStartDate,
    DateOnly? TenancyEndDate,
    Guid? ComparisonInspectionId,
    string? ComparisonReportNumber,
    DateTimeOffset? ComparisonCompletedAt);

public sealed record ReportPerson(Guid? UserId, string Name, string? Email);

public sealed record ReportPhoto(Guid MediaId, string StorageKey, string MimeType, string? Caption, DateTimeOffset UploadedAt, string? Sha256);

public sealed record ReportDefect(
    Guid Id,
    string? Title,
    string? Location,
    string Classification,
    string? Description,
    string? AiDescription,
    decimal? AiConfidence,
    IReadOnlyList<ReportPhoto> Photos);

public sealed record ReportRoomComparison(
    Guid SourceRoomId,
    string? BaselineDescription,
    IReadOnlyList<string> BaselineDefects,
    IReadOnlyList<ReportPhoto> BaselinePhotos,
    string? BasicComparison,
    string? AiAnalysis,
    string? Decision,
    string? Notes);

public sealed record ReportRoom(
    Guid Id,
    Guid OriginalPropertyRoomId,
    string Name,
    string RoomType,
    int Sequence,
    string? Description,
    string? AiDescription,
    Guid? AiAnalysisId,
    string DescriptionSource,
    string? AgentNotes,
    bool DefectsFound,
    IReadOnlyList<ReportPhoto> Photos,
    IReadOnlyList<ReportDefect> Defects,
    ReportRoomComparison? Comparison);

public sealed record ComparisonSummaryItem(string RoomName, string Decision, string? Notes);

public sealed record ReportComparisonSummary(
    Guid SourceInspectionId,
    string? SourceReportNumber,
    int RoomsCompared,
    IReadOnlyDictionary<string, int> DecisionCounts,
    IReadOnlyList<ComparisonSummaryItem> Items);

/// <summary>PDF rendering abstraction (QuestPDF implementation lives in Infrastructure).</summary>
public interface IPdfService
{
    /// <param name="images">Photo bytes keyed by storage key. Missing keys render as a placeholder.</param>
    byte[] RenderInspectionReport(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> images);
}

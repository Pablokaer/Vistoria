using System.Globalization;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Every fixed text of the report PDF in one language, chosen by the snapshot's report language (ADR 0014).
/// Dates are formatted here with our own month names: the app runs with invariant globalization, so no pt-BR
/// CultureInfo is available at runtime.
/// Example: <c>ReportPdfText.For("pt-BR").Label("MoveIn") // "Entrada"</c>.
/// </summary>
public sealed class ReportPdfText
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public required string Language { get; init; }
    public required string[] MonthNames { get; init; }
    /// <summary>{0} day, {1} month name, {2} year.</summary>
    public required string LongDateFormat { get; init; }
    /// <summary>Appended to the 3-letter month ("set." in Portuguese, nothing in English).</summary>
    public required string ShortMonthSuffix { get; init; }

    // Document metadata and cover.
    public required string DocumentTitleFormat { get; init; }
    public required string DocumentSubjectFormat { get; init; }
    public required string InspectionReport { get; init; }
    public required string ReportNumber { get; init; }
    public required string Version { get; init; }
    public required string Generated { get; init; }
    public required string PreparedBy { get; init; }
    public required string ImportantInformation { get; init; }
    public required string Disclaimer { get; init; }

    // Property summary.
    public required string PropertyType { get; init; }
    public required string InspectionType { get; init; }
    public required string InspectionDate { get; init; }
    public required string Inspector { get; init; }
    public required string Tenants { get; init; }
    public required string Tenancy { get; init; }
    public required string TenancyRangeFormat { get; init; }
    public required string Ongoing { get; init; }
    public required string ComparedWith { get; init; }
    public required string ComparedWithFormat { get; init; }
    public required string RoomsInspected { get; init; }

    // Rooms overview and room sections.
    public required string RoomsOverview { get; init; }
    public required string Room { get; init; }
    public required string Photos { get; init; }
    public required string Defects { get; init; }
    public required string Condition { get; init; }
    public required string NoDescription { get; init; }
    public required string NoPhotos { get; init; }
    public required string NoDefects { get; init; }
    public required string DefectsRecordedFormat { get; init; }
    public required string Defect { get; init; }
    public required string Location { get; init; }
    public required string InspectorNotes { get; init; }
    public required string PhotoCaptionFormat { get; init; }
    public required string DefectPhotoCaptionFormat { get; init; }
    public required string PhotoUnavailable { get; init; }

    // Move Out comparison.
    public required string ComparisonSummaryTitle { get; init; }
    public required string ComparisonTitle { get; init; }
    public required string Before { get; init; }
    public required string After { get; init; }
    public required string PreviousCondition { get; init; }
    public required string CurrentCondition { get; init; }
    public required string MoveInDefects { get; init; }
    public required string PossibleChanges { get; init; }
    public required string InspectorDecision { get; init; }
    public required string Notes { get; init; }

    // Inspection summary, report details, page chrome.
    public required string InspectionSummary { get; init; }
    public required string DefectsRecorded { get; init; }
    public required string RoomsWithoutDefects { get; init; }
    public required string RoomsWithDefects { get; init; }
    public required string DefectsByClassification { get; init; }
    public required string ComparisonOutcome { get; init; }
    public required string ReportDetails { get; init; }
    public required string ReportId { get; init; }
    public required string Company { get; init; }
    public required string Page { get; init; }
    public required string PageOf { get; init; }
    public required IReadOnlyDictionary<string, string> EnumLabels { get; init; }

    /// <summary>The texts for a language tag; unsupported tags get English.</summary>
    public static ReportPdfText For(string? language) =>
        SupportedLanguages.Resolve(language) == SupportedLanguages.PortugueseBrazil ? ReportPdfTexts.PortugueseBrazil : ReportPdfTexts.English;

    /// <summary>Label for an enum value stored in the snapshot, e.g. "MoveIn" → "Move In" / "Entrada".</summary>
    public string Label(string value) => EnumLabels.TryGetValue(value, out var label) ? label : value;

    /// <summary>"30 Sep 2026" / "30 set. 2026".</summary>
    public string ShortDate(DateOnly date) => $"{date.Day} {MonthNames[date.Month - 1][..3]}{ShortMonthSuffix} {date.Year}";

    public string ShortDate(DateTimeOffset value) => ShortDate(DateOnly.FromDateTime(value.UtcDateTime));

    /// <summary>"30 September 2026" / "30 de setembro de 2026".</summary>
    public string LongDate(DateTimeOffset value) => string.Format(Invariant, LongDateFormat, value.Day, MonthNames[value.Month - 1], value.Year);

    /// <summary>Short date plus UTC time, e.g. "30 Sep 2026 14:05".</summary>
    public string ShortDateTime(DateTimeOffset value) =>
        $"{ShortDate(value)} {value.UtcDateTime.ToString("HH:mm", Invariant)}";

    /// <summary>Fills a <c>{0}</c>-style format with invariant formatting. Example: <c>text.Format(text.PhotoCaptionFormat, 2)</c>.</summary>
    public string Format(string format, params object?[] values) => string.Format(Invariant, format, values);
}

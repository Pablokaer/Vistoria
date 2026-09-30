using System.Globalization;
using InspectFlow.Shared.Localization;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Every fixed text of the report PDF in one language, chosen by the snapshot's report language (ADR 0014).
/// Dates are formatted here with our own month names: the app runs with invariant globalization, so no pt-BR
/// CultureInfo is available at runtime.
/// Example: <c>ReportPdfText.For("pt-BR").ReportNumber("MI-0001") // "Laudo MI-0001"</c>.
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
    public required string ReportTitleFormat { get; init; }
    public required string DocumentTitleFormat { get; init; }
    public required string DocumentSubjectFormat { get; init; }
    public required string ReportNumberFormat { get; init; }
    public required string VersionFormat { get; init; }
    public required string GeneratedFormat { get; init; }
    public required string FooterReportIdFormat { get; init; }
    public required string Page { get; init; }
    public required string PageOf { get; init; }
    public required string Property { get; init; }
    public required string PropertyType { get; init; }
    public required string InspectionType { get; init; }
    public required string InspectionDate { get; init; }
    public required string Inspector { get; init; }
    public required string Tenants { get; init; }
    public required string Tenancy { get; init; }
    public required string Ongoing { get; init; }
    public required string ComparedWith { get; init; }
    public required string RoomsInspected { get; init; }
    public required string Disclaimer { get; init; }
    public required string ComparisonSummaryTitle { get; init; }
    public required string ComparisonSummaryFormat { get; init; }
    public required string Room { get; init; }
    public required string InspectorDecision { get; init; }
    public required string Notes { get; init; }
    public required string Condition { get; init; }
    public required string NoDescription { get; init; }
    public required string DefectsFormat { get; init; }
    public required string Defect { get; init; }
    public required string NoDefects { get; init; }
    public required string InspectorNotes { get; init; }
    public required string ComparisonWithMoveIn { get; init; }
    public required string MoveInRecord { get; init; }
    public required string MoveInDefects { get; init; }
    public required string PhotoUnavailable { get; init; }
    public required IReadOnlyDictionary<string, string> EnumLabels { get; init; }

    /// <summary>The texts for a language tag; unsupported tags get English.</summary>
    public static ReportPdfText For(string? language) =>
        SupportedLanguages.Resolve(language) == SupportedLanguages.PortugueseBrazil ? ReportPdfTexts.PortugueseBrazil : ReportPdfTexts.English;

    /// <summary>Label for an enum value stored in the snapshot, e.g. "MoveIn" → "Move In" / "Entrada".</summary>
    public string Label(string value) => EnumLabels.TryGetValue(value, out var label) ? label : value;

    /// <summary>"30 Sep 2026" / "30 set. 2026".</summary>
    public string ShortDate(DateOnly date) => $"{date.Day} {MonthNames[date.Month - 1][..3]}{ShortMonthSuffix} {date.Year}";

    /// <summary>"30 September 2026" / "30 de setembro de 2026".</summary>
    public string LongDate(DateTimeOffset value) => string.Format(Invariant, LongDateFormat, value.Day, MonthNames[value.Month - 1], value.Year);

    /// <summary>Short date plus UTC time, e.g. "30 Sep 2026 14:05".</summary>
    public string ShortDateTime(DateTimeOffset value) =>
        $"{ShortDate(DateOnly.FromDateTime(value.UtcDateTime))} {value.UtcDateTime.ToString("HH:mm", Invariant)}";

    public string ShortDate(DateTimeOffset value) => ShortDate(DateOnly.FromDateTime(value.UtcDateTime));

    /// <summary>Fills a <c>{0}</c>-style format with invariant formatting. Example: <c>text.Format(text.VersionFormat, 2)</c>.</summary>
    public string Format(string format, params object?[] values) => string.Format(Invariant, format, values);
}

using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// Closing section: headline figures, defects by classification, the Move In comparison table (Move Out) and the
/// report details that keep the document traceable (full report ID in small type here, not on every page).
/// </summary>
internal static class InspectionSummarySection
{
    public static void Render(ColumnDescriptor col, ReportSnapshot s, ReportPdfText text)
    {
        var stats = ReportStatistics.From(s);
        // The summary is one unit: kept on a single page so "Report details" is never left alone on the last page
        // (seen in the page-by-page review). Only a very long Move In comparison table may flow across pages.
        var summary = col.Item().PaddingTop(Space6);
        var keepTogether = (s.Comparison?.Items.Count ?? 0) <= SummaryKeepTogetherMaxRooms;
        (keepTogether ? summary.ShowEntire() : summary.EnsureSpace(200)).Column(all => RenderBody(all, s, stats, text));
    }

    private static void RenderBody(ColumnDescriptor col, ReportSnapshot s, ReportStatistics stats, ReportPdfText text)
    {
        col.Item().Column(block =>
        {
            block.Item().BorderBottom(0.6f).BorderColor(Line).PaddingBottom(Space2)
                .Text(text.InspectionSummary).FontSize(RoomTitleSize).Bold().FontColor(PdfTheme.Navy);
            block.Item().PaddingTop(Space3).Element(x => Metrics(x, stats, text));
            if (stats.DefectsByClassification.Count > 0)
                block.Item().PaddingTop(Space4).Element(x => Breakdown(x, text.DefectsByClassification, stats.DefectsByClassification, text));
            if (stats.ComparisonDecisions.Count > 0)
                block.Item().PaddingTop(Space4).Element(x => Breakdown(x, text.ComparisonOutcome, stats.ComparisonDecisions, text));
        });
        if (s.Comparison is { } comparison) col.Item().PaddingTop(Space4).Element(x => ComparisonTable(x, comparison, text));
        col.Item().PaddingTop(Space5).ShowEntire().Element(x => ReportDetails(x, s, text));
    }

    private static void Metrics(IContainer c, ReportStatistics stats, ReportPdfText text) => c.Row(row =>
    {
        row.Spacing(Space3);
        Metric(row.RelativeItem(), stats.RoomsInspected, text.RoomsInspected, PdfTheme.Navy);
        Metric(row.RelativeItem(), stats.DefectsRecorded, text.DefectsRecorded, stats.DefectsRecorded > 0 ? Danger : Success);
        Metric(row.RelativeItem(), stats.RoomsWithoutDefects, text.RoomsWithoutDefects, Success);
        if (stats.RoomsWithDefects > 0) Metric(row.RelativeItem(), stats.RoomsWithDefects, text.RoomsWithDefects, Danger);
    });

    private static void Metric(IContainer c, int value, string label, string color) =>
        c.Background(Surface).CornerRadius(Radius).Padding(PanelPadding).Column(col =>
        {
            col.Item().Text(value.ToString(CultureInfo.InvariantCulture)).FontSize(MetricSize).Bold().FontColor(color);
            col.Item().Text(label).FontSize(MetaSize).FontColor(Ink2);
        });

    private static void Breakdown(IContainer c, string title, IReadOnlyList<KeyValuePair<string, int>> counts, ReportPdfText text) => c.Column(col =>
    {
        col.Item().Element(x => PdfBlocks.Label(x, title));
        col.Item().PaddingTop(Space2).Row(row =>
        {
            row.Spacing(Space2);
            foreach (var (key, count) in counts)
                row.AutoItem().Element(x => PdfBlocks.Pill(x, $"{text.Label(key)}  {count.ToString(CultureInfo.InvariantCulture)}", PdfBlocks.ToneOf(key)));
        });
    });

    private static void ComparisonTable(IContainer c, ReportComparisonSummary summary, ReportPdfText text) => c.Column(col =>
    {
        col.Item().PaddingBottom(Space2).Element(x => PdfBlocks.Label(x, text.ComparisonSummaryTitle, PdfIcons.Compare));
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(4); });
            t.Header(h =>
            {
                foreach (var title in new[] { text.Room, text.InspectorDecision, text.Notes })
                    h.Cell().BorderBottom(0.6f).BorderColor(Line).PaddingVertical(Space1).Text(title).FontSize(LabelSize).SemiBold().FontColor(Muted);
            });
            foreach (var item in summary.Items)
            {
                t.Cell().BorderBottom(0.4f).BorderColor(Line).PaddingVertical(Space2 - 1).Text(item.RoomName).FontSize(MetaSize + 0.5f).SemiBold().FontColor(Ink);
                t.Cell().BorderBottom(0.4f).BorderColor(Line).PaddingVertical(Space2 - 1).AlignLeft().Element(x => PdfBlocks.Pill(x, text.Label(item.Decision), PdfBlocks.ToneOf(item.Decision)));
                t.Cell().BorderBottom(0.4f).BorderColor(Line).PaddingVertical(Space2 - 1).Text(PdfBlocks.Clean(item.Notes) ?? "—").FontSize(MetaSize).FontColor(Ink2);
            }
        });
    });

    private static void ReportDetails(IContainer c, ReportSnapshot s, ReportPdfText text) =>
        c.Border(0.6f).BorderColor(Line).CornerRadius(Radius).Padding(PanelPadding).Column(col =>
        {
            col.Item().Element(x => PdfBlocks.Label(x, text.ReportDetails, PdfIcons.Shield));
            col.Item().PaddingTop(Space2).Table(t =>
            {
                t.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                t.Cell().Element(x => PdfBlocks.Field(x, text.ReportNumber, s.ReportNumber));
                t.Cell().Element(x => PdfBlocks.Field(x, text.Version, s.VersionNumber.ToString(CultureInfo.InvariantCulture)));
                t.Cell().Element(x => PdfBlocks.Field(x, text.Generated, $"{text.ShortDateTime(s.GeneratedAt)} UTC"));
                t.Cell().ColumnSpan(3).PaddingTop(Space2).Element(x => PdfBlocks.Field(x, text.Company, CompanyLine(s.Company)));
            });
            col.Item().PaddingTop(Space2).Text($"{text.ReportId}: {s.ReportId}").FontSize(CaptionSize).FontColor(Faint);
        });

    private static string CompanyLine(ReportCompany company) =>
        string.Join(" · ", new[] { company.Name, company.ContactEmail, company.Phone }.Where(v => !string.IsNullOrWhiteSpace(v)));
}

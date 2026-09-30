using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// Page 1: logo + report identifiers, the inspection type and title, the property, a single property-summary panel,
/// the important-information callout and the rooms index. The domain stores no photo of the property as a whole,
/// so the cover uses a clean typographic composition instead of promoting a room photo to hero.
/// </summary>
internal static class CoverSection
{
    public static void Render(IContainer c, ReportSnapshot s, ReportPdfText text) => c.Column(col =>
    {
        col.Item().Element(x => TopBar(x, s, text));
        col.Item().PaddingTop(Space6).Element(x => TitleBlock(x, s, text));
        col.Item().PaddingTop(Space5).Element(x => PropertySummary(x, s, text));
        col.Item().PaddingTop(Space4).Element(x => PdfBlocks.Callout(x, text.ImportantInformation, text.Disclaimer));
        col.Item().PaddingTop(Space5).Element(x => RoomsOverview.Render(x, s, text));
    });

    private static void TopBar(IContainer c, ReportSnapshot s, ReportPdfText text) => c.Row(row =>
    {
        row.RelativeItem().AlignMiddle().Element(x => PdfIcons.Logo(x, 24));
        row.AutoItem().Element(x => Identifier(x, text.ReportNumber, s.ReportNumber));
        row.AutoItem().PaddingLeft(Space5).Element(x => Identifier(x, text.Version, s.VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        row.AutoItem().PaddingLeft(Space5).Element(x => Identifier(x, text.Generated, $"{text.ShortDateTime(s.GeneratedAt)} UTC"));
    });

    private static void Identifier(IContainer c, string label, string value) => c.AlignRight().Column(col =>
    {
        col.Item().AlignRight().Text(label).FontSize(LabelSize).FontColor(Muted);
        col.Item().AlignRight().Text(value).FontSize(MetaSize + 0.5f).SemiBold().FontColor(Ink);
    });

    private static string TypeColor(string type) => type switch
    {
        "MoveIn" => MoveIn, "MoveOut" => MoveOut, "Periodic" => Periodic, _ => Brand,
    };

    private static void TitleBlock(IContainer c, ReportSnapshot s, ReportPdfText text) =>
        c.BorderLeft(3).BorderColor(TypeColor(s.Inspection.Type)).PaddingLeft(Space4).Column(col =>
        {
            col.Item().Text(text.Label(s.Inspection.Type).ToUpperInvariant())
                .FontSize(EyebrowSize).Bold().LetterSpacing(0.12f).FontColor(TypeColor(s.Inspection.Type));
            col.Item().PaddingTop(Space1).Text(text.InspectionReport).FontSize(TitleSize).Bold().FontColor(PdfTheme.Navy);
            col.Item().PaddingTop(Space2).Text(s.Property.FullAddress).FontSize(AddressSize).FontColor(Ink2);
            col.Item().PaddingTop(Space2).Text(t =>
            {
                t.Span($"{text.PreparedBy} ").FontSize(MetaSize).FontColor(Muted);
                t.Span(s.Company.Name).FontSize(MetaSize).SemiBold().FontColor(Ink2);
            });
        });

    /// <summary>Only facts that exist are listed: no empty labels for a missing tenant, inspector or tenancy.</summary>
    private static List<(string Icon, string Label, string Value)> SummaryFacts(ReportSnapshot s, ReportPdfText text)
    {
        var facts = new List<(string, string, string)>
        {
            (PdfIcons.Building, text.PropertyType, text.Label(s.Property.PropertyType)),
            (PdfIcons.Clipboard, text.InspectionType, text.Label(s.Inspection.Type)),
            (PdfIcons.Calendar, text.InspectionDate, text.LongDate(s.Inspection.CompletedAt)),
            (PdfIcons.Rooms, text.RoomsInspected, s.Rooms.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        if (s.Agent is not null) facts.Add((PdfIcons.User, text.Inspector, s.Agent.Name));
        if (s.Tenants.Count > 0) facts.Add((PdfIcons.Users, text.Tenants, string.Join(", ", s.Tenants.Select(t => t.Name))));
        if (s.Inspection.TenancyStartDate is { } start)
        {
            var end = s.Inspection.TenancyEndDate is { } e ? text.ShortDate(e) : text.Ongoing;
            facts.Add((PdfIcons.Key, text.Tenancy, text.Format(text.TenancyRangeFormat, text.ShortDate(start), end)));
        }
        if (s.Inspection.ComparisonReportNumber is { } baseline)
        {
            var on = s.Inspection.ComparisonCompletedAt is { } at ? $" · {text.ShortDate(at)}" : "";
            facts.Add((PdfIcons.Compare, text.ComparedWith, text.Format(text.ComparedWithFormat, baseline) + on));
        }
        return facts;
    }

    /// <summary>One panel, two columns of facts — not a card per fact.</summary>
    private static void PropertySummary(IContainer c, ReportSnapshot s, ReportPdfText text)
    {
        var facts = SummaryFacts(s, text);
        c.Border(0.6f).BorderColor(Line).CornerRadius(Radius).Padding(PanelPadding + 2).Table(table =>
        {
            table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
            foreach (var (icon, label, value) in facts)
                table.Cell().PaddingVertical(Space2).PaddingRight(Space4).Element(x => PdfBlocks.Field(x, label, value, icon));
        });
    }
}

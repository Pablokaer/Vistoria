using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>Compact header and footer of the inner pages: they identify the report without taking space from evidence.</summary>
internal static class PageChrome
{
    public static void Header(IContainer c, ReportSnapshot s, ReportPdfText text) =>
        c.PaddingBottom(Space4).BorderBottom(0.6f).BorderColor(Line).PaddingBottom(Space2).Row(row =>
        {
            row.AutoItem().AlignMiddle().Element(x => PdfIcons.Logo(x, 14));
            row.RelativeItem().AlignRight().AlignMiddle().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(MetaSize - 0.5f).FontColor(Muted));
                t.Span($"{text.ReportNumber} ").FontColor(Faint);
                t.Span(s.ReportNumber).SemiBold().FontColor(Ink2);
                t.Span($"   ·   {s.Property.FullAddress}");
            });
        });

    /// <summary>InspectFlow · report number · Page X of Y. The full technical report ID lives in the report details.</summary>
    public static void Footer(IContainer c, ReportSnapshot s, ReportPdfText text) =>
        c.PaddingTop(Space2).BorderTop(0.6f).BorderColor(Line).PaddingTop(Space2).Row(row =>
        {
            row.RelativeItem().AlignLeft().Text("InspectFlow").FontSize(CaptionSize + 0.5f).SemiBold().FontColor(PdfTheme.Navy);
            row.RelativeItem().AlignCenter().Text(s.ReportNumber).FontSize(CaptionSize + 0.5f).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(CaptionSize + 0.5f).FontColor(Muted));
                t.Span(text.Page);
                t.CurrentPageNumber();
                t.Span(text.PageOf);
                t.TotalPages();
            });
        });
}

using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// Defects of a room. Absent defects are already stated by the condition status pill, so nothing is added; present
/// defects get more weight: accent bar, number, title, location, classification, description and their photos.
/// </summary>
internal static class DefectSection
{
    public static void Render(ColumnDescriptor col, ReportRoom room, ReportImages images, ReportPdfText text)
    {
        if (room.Defects.Count == 0) return;
        var index = 0;
        foreach (var defect in room.Defects)
        {
            var number = ++index;
            var rows = PhotoRows(defect, number, text);
            col.Item().PaddingTop(Space4).EnsureSpace(DefectMinSpace).Element(x => Card(x, defect, number, rows, images, text));
        }
    }

    private static string AccentOf(string classification) => PdfBlocks.ToneOf(classification) switch
    {
        PdfBlocks.Tone.Danger => Danger,
        PdfBlocks.Tone.Warning => PdfTheme.Warning,
        _ => Faint,
    };

    /// <summary>Defect photos two per row (half width), captioned "Defect 1 · photo 2" so they can be referred to.</summary>
    private static IReadOnlyList<CaptionedPhoto[]> PhotoRows(ReportDefect defect, int number, ReportPdfText text) =>
        defect.Photos.Select((p, i) => new CaptionedPhoto(p, text.Format(text.DefectPhotoCaptionFormat, number, i + 1))).Chunk(2).ToList();

    /// <summary>
    /// Title row, then the description beside the photos (up to two) so a defect stays compact; with more photos
    /// the description comes first and the photos follow two per row.
    /// </summary>
    private static void Card(IContainer c, ReportDefect defect, int number, IReadOnlyList<CaptionedPhoto[]> rows, ReportImages images, ReportPdfText text) =>
        c.BorderLeft(2.5f).BorderColor(AccentOf(defect.Classification)).PaddingLeft(Space3).Column(col =>
        {
            col.Item().Element(x => Title(x, defect, number, text));
            var description = PdfBlocks.Clean(defect.Description) ?? "—";
            if (rows.Count == 1 && description.Length <= KeepWithPhotosMaxChars)
            {
                col.Item().PaddingTop(Space2).ShowEntire().Row(row =>
                {
                    row.Spacing(Space4);
                    row.RelativeItem(2).Text(description).Style(Body);
                    row.RelativeItem(3).Element(x => PhotoGrid.Columns(x, rows[0], images, text, rows[0].Length, PhotoDefect));
                });
                return;
            }
            col.Item().PaddingTop(Space2).Text(description).Style(Body);
            foreach (var row in rows)
                col.Item().PaddingTop(PhotoGap).ShowEntire().Element(x => PhotoGrid.Columns(x, row, images, text, 2, PhotoDefect));
        });

    private static void Title(IContainer c, ReportDefect defect, int number, ReportPdfText text) => c.Row(row =>
    {
        row.AutoItem().AlignMiddle().Element(x => PdfIcons.Sized(x, PdfIcons.Alert, AccentOf(defect.Classification), 11));
        row.RelativeItem().PaddingLeft(Space2).AlignMiddle().Text(t =>
        {
            t.Span($"{text.Defect} {number.ToString(CultureInfo.InvariantCulture)} · ").FontSize(BodySize).SemiBold().FontColor(Muted);
            t.Span(PdfBlocks.Clean(defect.Title) ?? text.Defect).FontSize(BodySize + 0.5f).Bold().FontColor(Ink);
            if (PdfBlocks.Clean(defect.Location) is { } location)
                t.Span($"   {text.Location}: {location}").FontSize(MetaSize).FontColor(Muted);
        });
        row.AutoItem().AlignMiddle().Element(x => PdfBlocks.Pill(x, text.Label(defect.Classification), PdfBlocks.ToneOf(defect.Classification)));
    });
}

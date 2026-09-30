using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// One room as a self-contained section. It is added to the document column as several pagination units, so rooms
/// share pages when they fit and long rooms flow across pages without orphaned pieces:
/// header + first photo row (kept together) → further photo rows → condition → defects → notes.
/// </summary>
internal static class RoomSection
{
    public static void Render(ColumnDescriptor col, ReportRoom room, ReportImages images, ReportPdfText text)
    {
        if (room.Comparison is { } comparison)
        {
            ComparisonSection.Render(col, room, comparison, images, text);
        }
        else
        {
            Standard(col, room, images, text);
        }
        DefectSection.Render(col, room, images, text);
        if (PdfBlocks.Clean(room.AgentNotes) is { } notes)
            col.Item().PaddingTop(Space3).EnsureSpace(TextMinSpace / 2).Element(x => Notes(x, notes, text));
    }

    /// <summary>A short condition text is kept on the same page as the photos it describes.</summary>
    private static bool KeepsWithPhotos(string? text) => (text?.Length ?? 0) <= KeepWithPhotosMaxChars;

    /// <summary>
    /// Pagination units: [header + first row] … [last row + condition]. A single photo sits beside a short condition
    /// (one compact unit); a long condition flows on after the photos instead of forcing a page break.
    /// </summary>
    private static void Standard(ColumnDescriptor col, ReportRoom room, ReportImages images, ReportPdfText text)
    {
        var rows = PhotoGrid.Rows(Captioned(room.Photos, text));
        var keep = KeepsWithPhotos(room.Description);
        if (rows.Count == 1 && rows[0].Kind == PhotoRowKind.Single && keep)
        {
            col.Item().EnsureSpace(RoomMinSpace).ShowEntire().Column(block =>
            {
                block.Item().Element(x => Header(x, room, text));
                block.Item().PaddingTop(Space3).Element(x => Beside(x, rows[0].Photos[0], room, images, text));
            });
            return;
        }
        var conditionWithLastRow = keep && rows.Count > 0;
        col.Item().EnsureSpace(RoomMinSpace).ShowEntire().Column(block =>
        {
            block.Item().Element(x => Header(x, room, text));
            block.Item().PaddingTop(Space3).Element(x => FirstRow(x, rows, images, text));
            if (conditionWithLastRow && rows.Count == 1) block.Item().PaddingTop(Space4).Element(x => Condition(x, room.Description, room, text, text.Condition));
        });
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var last = i == rows.Count - 1;
            col.Item().PaddingTop(PhotoGap).ShowEntire().Column(block =>
            {
                block.Item().Element(x => PhotoGrid.RenderRow(x, row, images, text));
                if (last && conditionWithLastRow) block.Item().PaddingTop(Space4).Element(x => Condition(x, room.Description, room, text, text.Condition));
            });
        }
        if (!conditionWithLastRow)
            col.Item().PaddingTop(Space4).EnsureSpace(TextMinSpace).Element(x => Condition(x, room.Description, room, text, text.Condition));
    }

    /// <summary>One photo (left, ~60%) with the condition beside it — no half-empty full-width photo band.</summary>
    private static void Beside(IContainer c, CaptionedPhoto photo, ReportRoom room, ReportImages images, ReportPdfText text) => c.Row(row =>
    {
        row.Spacing(Space4);
        PhotoGrid.Cell(row.RelativeItem(3).Height(PhotoBeside), photo, images, text);
        row.RelativeItem(2).Element(x => Condition(x, room.Description, room, text, text.Condition));
    });

    public static IReadOnlyList<CaptionedPhoto> Captioned(IReadOnlyList<ReportPhoto> photos, ReportPdfText text) =>
        photos.Select((p, i) => new CaptionedPhoto(p, text.Format(text.PhotoCaptionFormat, i + 1))).ToList();

    /// <summary>[01]  Room name ……… room type — number and title in deep navy, the type discreet on the right.</summary>
    public static void Header(IContainer c, ReportRoom room, ReportPdfText text) =>
        c.Section(RoomsOverview.SectionName(room)).BorderBottom(0.6f).BorderColor(Line).PaddingBottom(Space2).Row(row =>
        {
            row.AutoItem().AlignMiddle().Width(26).Height(22).Background(PdfTheme.Navy).CornerRadius(Radius).AlignCenter().AlignMiddle()
                .Text(room.Sequence.ToString("00", CultureInfo.InvariantCulture)).FontSize(MetaSize + 0.5f).Bold().FontColor("#FFFFFF");
            row.RelativeItem().PaddingLeft(Space3).AlignMiddle().Text(room.Name).FontSize(RoomTitleSize).Bold().FontColor(PdfTheme.Navy);
            row.AutoItem().AlignMiddle().Row(type =>
            {
                type.AutoItem().AlignMiddle().PaddingRight(Space1).Element(x => PdfIcons.Sized(x, PdfIcons.Rooms, Faint, 10));
                type.AutoItem().AlignMiddle().Text(text.Label(room.RoomType)).FontSize(MetaSize).FontColor(Muted);
            });
        });

    private static void FirstRow(IContainer c, IReadOnlyList<PhotoRow> rows, ReportImages images, ReportPdfText text)
    {
        if (rows.Count > 0) PhotoGrid.RenderRow(c, rows[0], images, text);
        else NoPhotos(c, text);
    }

    public static void NoPhotos(IContainer c, ReportPdfText text) => c.Background(Surface).CornerRadius(Radius).Padding(Space3).Row(r =>
    {
        r.AutoItem().AlignMiddle().Element(x => PdfIcons.Sized(x, PdfIcons.Camera, Faint, 12));
        r.AutoItem().PaddingLeft(Space2).AlignMiddle().Text(text.NoPhotos).FontSize(MetaSize).FontColor(Muted);
    });

    /// <summary>
    /// Label row ("CONDITION" + the room's defect status on the right, so the status never ends up alone on a page),
    /// then the text confirmed by the inspector, shown as recorded.
    /// </summary>
    public static void Condition(IContainer c, string? description, ReportRoom room, ReportPdfText text, string label) => c.Column(col =>
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().AlignMiddle().Element(x => PdfBlocks.Label(x, label, PdfIcons.Clipboard));
            row.AutoItem().Element(x => DefectStatus(x, room, text));
        });
        col.Item().PaddingTop(Space2).Element(x => PdfBlocks.Paragraph(x, description, text.NoDescription));
    });

    public static void DefectStatus(IContainer c, ReportRoom room, ReportPdfText text)
    {
        if (room.Defects.Count == 0) PdfBlocks.Pill(c, text.NoDefects, PdfBlocks.Tone.Success, PdfIcons.Check);
        else PdfBlocks.Pill(c, text.Format(text.DefectsRecordedFormat, room.Defects.Count), PdfBlocks.Tone.Danger, PdfIcons.Alert);
    }

    private static void Notes(IContainer c, string notes, ReportPdfText text) => c.Column(col =>
    {
        col.Item().Element(x => PdfBlocks.Label(x, text.InspectorNotes, PdfIcons.Notes));
        col.Item().PaddingTop(Space1).Text(notes).Style(Body);
    });
}

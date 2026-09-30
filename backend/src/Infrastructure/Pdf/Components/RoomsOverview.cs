using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// Index of rooms on the cover: number, name, photo and defect counts (plus the inspector's decision for Move Out)
/// and the page where the room starts, linked to its section — the reader can scan and jump.
/// </summary>
internal static class RoomsOverview
{
    public static string SectionName(ReportRoom room) => $"room-{room.Id:N}";

    public static void Render(IContainer c, ReportSnapshot s, ReportPdfText text)
    {
        var withDecision = s.Rooms.Any(r => r.Comparison is not null);
        c.Column(col =>
        {
            col.Item().PaddingBottom(Space2).Element(x => PdfBlocks.Label(x, text.RoomsOverview, PdfIcons.Rooms));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(26); cols.RelativeColumn(3); cols.ConstantColumn(48); cols.RelativeColumn(2);
                    if (withDecision) cols.RelativeColumn(2);
                    cols.ConstantColumn(30);
                });
                table.Header(h => HeaderRow(h, text, withDecision));
                foreach (var room in s.Rooms.OrderBy(r => r.Sequence)) Row(table, room, text, withDecision);
            });
        });
    }

    private static void HeaderRow(TableCellDescriptor h, ReportPdfText text, bool withDecision)
    {
        var titles = new List<string> { "#", text.Room, text.Photos, text.Defects };
        if (withDecision) titles.Add(text.InspectorDecision);
        titles.Add("");
        foreach (var title in titles)
            h.Cell().BorderBottom(0.6f).BorderColor(Line).PaddingVertical(Space1).Text(title).FontSize(LabelSize).SemiBold().FontColor(Muted);
    }

    private static void Row(TableDescriptor table, ReportRoom room, ReportPdfText text, bool withDecision)
    {
        IContainer Cell() => table.Cell().BorderBottom(0.4f).BorderColor(Line).PaddingVertical(Space2 - 1).AlignMiddle();
        Cell().Text(room.Sequence.ToString("00", CultureInfo.InvariantCulture)).FontSize(MetaSize).SemiBold().FontColor(Brand);
        Cell().SectionLink(SectionName(room)).Text(room.Name).FontSize(BodySize).SemiBold().FontColor(Ink);
        Cell().Text(room.Photos.Count.ToString(CultureInfo.InvariantCulture)).FontSize(MetaSize).FontColor(Ink2);
        Cell().Element(x => DefectsCell(x, room, text));
        if (withDecision) Cell().Element(x => DecisionCell(x, room, text));
        Cell().AlignRight().SectionLink(SectionName(room)).Text(t =>
        {
            t.BeginPageNumberOfSection(SectionName(room)).FontSize(MetaSize).FontColor(Muted);
        });
    }

    private static void DefectsCell(IContainer c, ReportRoom room, ReportPdfText text)
    {
        if (room.Defects.Count == 0) c.Row(r => { r.AutoItem().Element(x => PdfIcons.Sized(x, PdfIcons.Check, Success, 9)); r.AutoItem().PaddingLeft(Space1).Text(text.NoDefects).FontSize(MetaSize - 0.5f).FontColor(Success); });
        else c.Row(r => { r.AutoItem().Element(x => PdfIcons.Sized(x, PdfIcons.Alert, Danger, 9)); r.AutoItem().PaddingLeft(Space1).Text(text.Format(text.DefectsRecordedFormat, room.Defects.Count)).FontSize(MetaSize - 0.5f).SemiBold().FontColor(Danger); });
    }

    private static void DecisionCell(IContainer c, ReportRoom room, ReportPdfText text)
    {
        if (room.Comparison?.Decision is { } decision) c.AlignLeft().Element(x => PdfBlocks.Pill(x, text.Label(decision), PdfBlocks.ToneOf(decision)));
        else c.Text("—").FontSize(MetaSize).FontColor(Faint);
    }
}

using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>
/// A Move Out room compared with its Move In record: Before | After side by side (photos, then conditions), the
/// recorded differences and the inspector's decision. The room's own photos appear once — the first two in the
/// After column, the rest in a grid below — so evidence is never duplicated.
/// </summary>
internal static class ComparisonSection
{
    private const int SideBySidePhotos = 2;

    public static void Render(ColumnDescriptor col, ReportRoom room, ReportRoomComparison cmp, ReportImages images, ReportPdfText text)
    {
        var current = RoomSection.Captioned(room.Photos, text);
        var before = cmp.BaselinePhotos.Select((p, i) => new CaptionedPhoto(p, $"{text.Label("MoveIn")} · {text.Format(text.PhotoCaptionFormat, i + 1)}")).ToList();
        col.Item().EnsureSpace(RoomMinSpace).ShowEntire().Column(block =>
        {
            block.Item().Element(x => RoomSection.Header(x, room, text));
            block.Item().PaddingTop(Space3).Element(x => PdfBlocks.Label(x, text.ComparisonTitle, PdfIcons.Compare, Brand));
            block.Item().PaddingTop(Space2).Row(row =>
            {
                row.Spacing(Space4);
                row.RelativeItem().Element(x => Side(x, text.Before, before, images, text, muted: true));
                row.RelativeItem().Element(x => Side(x, text.After, current, images, text, muted: false));
            });
        });
        col.Item().PaddingTop(Space3).EnsureSpace(TextMinSpace).Row(row =>
        {
            row.Spacing(Space4);
            row.RelativeItem().Element(x => Previous(x, cmp, text));
            row.RelativeItem().Element(x => RoomSection.Condition(x, room.Description, room, text, text.CurrentCondition));
        });
        // Changes and decision are short and belong together: one unbreakable block, so the decision is never
        // orphaned at the top of the next page, away from its room (seen in the page-by-page review).
        col.Item().PaddingTop(Space3).ShowEntire().Column(outcome =>
        {
            if (PdfBlocks.Clean(cmp.BasicComparison) is { } changes)
                outcome.Item().PaddingBottom(Space3).Element(x => PossibleChanges(x, changes, text));
            outcome.Item().Element(x => Decision(x, cmp, text));
        });
        foreach (var row in PhotoGrid.Rows(current.Skip(SideBySidePhotos).ToList()))
            col.Item().PaddingTop(PhotoGap).ShowEntire().Element(x => PhotoGrid.RenderRow(x, row, images, text));
    }

    private static void Side(IContainer c, string title, IReadOnlyList<CaptionedPhoto> photos, ReportImages images, ReportPdfText text, bool muted) => c.Column(col =>
    {
        col.Item().Element(x => PdfBlocks.Pill(x, title, muted ? PdfBlocks.Tone.Neutral : PdfBlocks.Tone.Brand));
        var shown = photos.Take(SideBySidePhotos).ToList();
        col.Item().PaddingTop(Space2).Element(x =>
        {
            if (shown.Count == 0) RoomSection.NoPhotos(x, text);
            else if (shown.Count == 1) PhotoGrid.Cell(x.Height(PhotoComparison), shown[0], images, text);
            else PhotoGrid.Columns(x, shown, images, text, SideBySidePhotos, PhotoComparison);
        });
        if (muted && photos.Count > SideBySidePhotos)
            col.Item().PaddingTop(Space1).Text($"+{photos.Count - SideBySidePhotos} · {text.Photos}").FontSize(CaptionSize).FontColor(Muted);
    });

    private static void Previous(IContainer c, ReportRoomComparison cmp, ReportPdfText text) => c.Column(col =>
    {
        col.Item().Element(x => PdfBlocks.Label(x, text.PreviousCondition, PdfIcons.Clipboard));
        col.Item().PaddingTop(Space2).Element(x => PdfBlocks.Paragraph(x, cmp.BaselineDescription, text.NoDescription));
        if (cmp.BaselineDefects.Count == 0) return;
        col.Item().PaddingTop(Space2).Text(text.MoveInDefects).FontSize(LabelSize).SemiBold().FontColor(Muted);
        foreach (var defect in cmp.BaselineDefects)
            col.Item().Text($"•  {PdfBlocks.Clean(defect)}").FontSize(MetaSize).FontColor(Ink2);
    });

    private static void PossibleChanges(IContainer c, string changes, ReportPdfText text) =>
        c.Background(Surface).CornerRadius(Radius).Padding(PanelPadding).Column(col =>
        {
            col.Item().Element(x => PdfBlocks.Label(x, text.PossibleChanges, PdfIcons.Compare));
            col.Item().PaddingTop(Space1).Text(changes).FontSize(MetaSize).FontColor(Ink2).LineHeight(1.35f);
        });

    private static void Decision(IContainer c, ReportRoomComparison cmp, ReportPdfText text) =>
        c.Border(0.6f).BorderColor(Line).CornerRadius(Radius).Padding(PanelPadding).Row(row =>
        {
            row.AutoItem().AlignMiddle().Element(x => PdfBlocks.Label(x, text.InspectorDecision, PdfIcons.Shield, PdfTheme.Navy));
            var decision = cmp.Decision ?? "Pending";
            row.AutoItem().PaddingLeft(Space3).AlignMiddle().Element(x => PdfBlocks.Pill(x, text.Label(decision), PdfBlocks.ToneOf(decision)));
            if (PdfBlocks.Clean(cmp.Notes) is { } notes)
                row.RelativeItem().PaddingLeft(Space4).AlignMiddle().Text(notes).FontSize(MetaSize + 0.5f).FontColor(Ink2);
        });
}

using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>A photo with the caption that identifies it as evidence ("Photo 2", "Defect 1 · photo 1").</summary>
internal sealed record CaptionedPhoto(ReportPhoto Photo, string Caption);

/// <summary>One row of the photo layout; rows are the unit of pagination (a row never splits across pages).</summary>
internal sealed record PhotoRow(IReadOnlyList<CaptionedPhoto> Photos, PhotoRowKind Kind);

internal enum PhotoRowKind { Single, Pair, Feature, Thirds }

/// <summary>
/// Photo layouts by count: 1 wide, 2 side by side, 3 = one feature + two stacked, 4 = 2×2, more = rows of three.
/// Photos are always contained in their cell (aspect ratio kept, never stretched or cropped).
/// </summary>
internal static class PhotoGrid
{
    /// <summary>Splits photos into rows. Example: 4 photos → two Pair rows (a 2×2 grid).</summary>
    public static IReadOnlyList<PhotoRow> Rows(IReadOnlyList<CaptionedPhoto> photos) => photos.Count switch
    {
        0 => [],
        1 => [new PhotoRow(photos, PhotoRowKind.Single)],
        2 => [new PhotoRow(photos, PhotoRowKind.Pair)],
        3 => [new PhotoRow(photos, PhotoRowKind.Feature)],
        4 => [new PhotoRow(photos.Take(2).ToList(), PhotoRowKind.Pair), new PhotoRow(photos.Skip(2).ToList(), PhotoRowKind.Pair)],
        _ => photos.Chunk(3).Select(chunk => new PhotoRow(chunk, PhotoRowKind.Thirds)).ToList(),
    };

    public static void RenderRow(IContainer c, PhotoRow row, ReportImages images, ReportPdfText text)
    {
        switch (row.Kind)
        {
            case PhotoRowKind.Single: Cell(c.Height(PhotoSingle), row.Photos[0], images, text); break;
            case PhotoRowKind.Feature: Feature(c, row.Photos, images, text); break;
            default: Columns(c, row.Photos, images, text, row.Kind == PhotoRowKind.Pair ? 2 : 3); break;
        }
    }

    /// <summary>Equal columns; a short last row keeps the same cell width so the grid stays consistent.</summary>
    public static void Columns(IContainer c, IReadOnlyList<CaptionedPhoto> photos, ReportImages images, ReportPdfText text, int columns, float? height = null) =>
        c.Row(r =>
        {
            r.Spacing(PhotoGap);
            var cellHeight = height ?? (columns == 2 ? PhotoHalf : PhotoThird);
            for (var i = 0; i < columns; i++)
            {
                var item = r.RelativeItem();
                if (i < photos.Count) Cell(item.Height(cellHeight), photos[i], images, text);
            }
        });

    private static void Feature(IContainer c, IReadOnlyList<CaptionedPhoto> photos, ReportImages images, ReportPdfText text) => c.Row(r =>
    {
        r.Spacing(PhotoGap);
        Cell(r.RelativeItem(1.62f).Height(PhotoFeature), photos[0], images, text);
        r.RelativeItem().Column(col =>
        {
            col.Spacing(PhotoGap);
            var small = (PhotoFeature - PhotoGap) / 2;
            Cell(col.Item().Height(small), photos[1], images, text);
            Cell(col.Item().Height(small), photos[2], images, text);
        });
    });

    /// <summary>One photo on a light backdrop (so letterboxing looks intentional) with its caption overlaid bottom-left.</summary>
    public static void Cell(IContainer c, CaptionedPhoto item, ReportImages images, ReportPdfText text) =>
        c.Background(Surface).CornerRadius(Radius).Layers(layers =>
        {
            var image = images.Find(item.Photo);
            if (image is not null) layers.PrimaryLayer().AlignCenter().AlignMiddle().Image(image).FitArea();
            else layers.PrimaryLayer().AlignCenter().AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Element(x => PdfIcons.Sized(x, PdfIcons.Camera, Faint, 18));
                col.Item().PaddingTop(Space1).Text(text.PhotoUnavailable).FontSize(CaptionSize).FontColor(Muted);
            });
            layers.Layer().AlignBottom().AlignLeft().Padding(Space1)
                .Background("#E6FFFFFF").CornerRadius(2).PaddingHorizontal(Space1).PaddingVertical(1)
                .Text(item.Caption).FontSize(CaptionSize).SemiBold().FontColor(Ink2);
        });
}

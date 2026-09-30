using InspectFlow.Infrastructure.Pdf.Components;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Renders the immutable report snapshot to an A4 PDF: a cover/executive summary page, then the rooms as
/// content-driven sections (rooms share pages when they fit), then the inspection summary. Fixed texts follow the
/// snapshot's report language (<see cref="ReportPdfText"/>); content typed by people is printed as recorded.
/// Layout tokens live in <see cref="PdfTheme"/>, sections in Components/.
/// </summary>
public sealed class QuestPdfReportService : IPdfService
{
    public byte[] RenderInspectionReport(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> images) =>
        Build(snapshot, images).GeneratePdf();

    /// <summary>Every page as PNG — for visual review of the layout (tests), not used by the application.</summary>
    public IReadOnlyList<byte[]> RenderPreviewImages(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> images) =>
        Build(snapshot, images).GenerateImages(new ImageGenerationSettings { RasterDpi = 110 }).ToList();

    private static Document Build(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> bytes)
    {
        var text = ReportPdfText.For(snapshot.Language);
        var images = new ReportImages(bytes);
        return Document.Create(container =>
        {
            container.Page(page => CoverPage(page, snapshot, text));
            container.Page(page => ContentPages(page, snapshot, images, text));
        }).WithMetadata(Metadata(snapshot, text));
    }

    private static void Setup(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(PageMarginX);
        page.MarginVertical(PageMarginY);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(t => t.FontFamily(FontFamily).FontSize(BodySize).FontColor(Ink2));
    }

    private static void CoverPage(PageDescriptor page, ReportSnapshot s, ReportPdfText text)
    {
        Setup(page);
        page.Content().PaddingTop(Space2).Element(c => CoverSection.Render(c, s, text));
        page.Footer().Element(c => PageChrome.Footer(c, s, text));
    }

    private static void ContentPages(PageDescriptor page, ReportSnapshot s, ReportImages images, ReportPdfText text)
    {
        Setup(page);
        page.Header().Element(c => PageChrome.Header(c, s, text));
        page.Content().Column(col =>
        {
            var first = true;
            foreach (var room in s.Rooms.OrderBy(r => r.Sequence))
            {
                // Rooms flow one after another; the gap separates them, EnsureSpace decides the page breaks.
                if (!first) col.Item().PaddingTop(Space6);
                first = false;
                RoomSection.Render(col, room, images, text);
            }
            InspectionSummarySection.Render(col, s, text);
        });
        page.Footer().Element(c => PageChrome.Footer(c, s, text));
    }

    private static DocumentMetadata Metadata(ReportSnapshot s, ReportPdfText text) => new()
    {
        Title = text.Format(text.DocumentTitleFormat, s.ReportNumber),
        Author = s.Company.Name,
        Subject = text.Format(text.DocumentSubjectFormat, text.Label(s.Inspection.Type), s.Property.FullAddress),
        Creator = "InspectFlow",
        CreationDate = s.GeneratedAt,
        ModifiedDate = s.GeneratedAt,
    };
}

using InspectFlow.Infrastructure.Pdf;
using InspectFlow.Tests.Support;
using QuestPDF.Infrastructure;

namespace InspectFlow.Tests.Domain;

/// <summary>
/// Renders every report scenario. QuestPDF throws on impossible layouts (content that can never fit a page), so
/// "renders" is a real layout check. Set PDF_PREVIEW_DIR to also write each PDF and its pages as PNG for review.
/// </summary>
public class ReportPdfRenderingTests
{
    static ReportPdfRenderingTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public static TheoryData<string> Scenarios() => new(ReportSamples.All.Keys);

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_scenario_renders_a_valid_pdf(string scenario)
    {
        var pdf = new QuestPdfReportService().RenderInspectionReport(ReportSamples.All[scenario](), ReportSamples.SeedPhotos);

        Assert.True(pdf.Length > 10_000, $"{scenario}: PDF is suspiciously small ({pdf.Length} bytes).");
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
        WritePreview(scenario, pdf);
    }

    [Fact]
    public void Missing_photo_bytes_render_a_placeholder_instead_of_failing()
    {
        var pdf = new QuestPdfReportService().RenderInspectionReport(ReportSamples.MoveInWithDefects(), new Dictionary<string, byte[]>());
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }

    [Fact]
    public void Move_out_reports_load_the_move_in_photos_for_the_before_column()
    {
        var snapshot = ReportSamples.MoveOutComparison();
        var baseline = snapshot.Rooms.SelectMany(r => r.Comparison?.BaselinePhotos ?? []).Select(p => p.StorageKey).ToList();
        Assert.NotEmpty(baseline);

        var keys = InspectFlow.Modules.Reports.Application.ReportImageLoader.PhotoKeys(snapshot);
        Assert.All(baseline, key => Assert.Contains(key, keys));
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    private static void WritePreview(string scenario, byte[] pdf)
    {
        var dir = Environment.GetEnvironmentVariable("PDF_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(dir)) return;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, $"{scenario}.pdf"), pdf);
        var pages = new QuestPdfReportService().RenderPreviewImages(ReportSamples.All[scenario](), ReportSamples.SeedPhotos);
        for (var i = 0; i < pages.Count; i++)
            File.WriteAllBytes(Path.Combine(dir, $"{scenario}-p{i + 1}.png"), pages[i]);
    }
}

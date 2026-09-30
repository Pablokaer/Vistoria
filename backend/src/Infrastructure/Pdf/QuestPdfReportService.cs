using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Renders the immutable report snapshot to a professional A4 PDF. Fixed texts follow the snapshot's report language
/// (<see cref="ReportPdfText"/>); content typed by people or drafted by AI is printed as recorded.
/// </summary>
public sealed class QuestPdfReportService : IPdfService
{
    private const string Primary = "#0F4C5C";
    private const string Muted = "#5B6770";
    private const string Border = "#D9DEE2";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public byte[] RenderInspectionReport(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> images)
    {
        var s = snapshot;
        var text = ReportPdfText.For(s.Language);
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor("#1F2A30"));
            page.Header().Element(c => Header(c, s, text));
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);
                col.Item().Element(c => Summary(c, s, text));
                col.Item().Element(c => Disclaimer(c, text));
                if (s.Comparison is not null) col.Item().Element(c => ComparisonSummary(c, s.Comparison, text));
                foreach (var room in s.Rooms.OrderBy(r => r.Sequence))
                    col.Item().Element(c => Room(c, room, images, text));
            });
            page.Footer().Element(c => Footer(c, s, text));
        }))
        .WithMetadata(Metadata(s, text))
        .GeneratePdf();
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

    private static void Header(IContainer c, ReportSnapshot s, ReportPdfText text) => c.BorderBottom(1).BorderColor(Primary).PaddingBottom(8).Row(row =>
    {
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(s.Company.Name).FontSize(15).Bold().FontColor(Primary);
            col.Item().Text(text.Format(text.ReportTitleFormat, text.Label(s.Inspection.Type))).FontSize(11).SemiBold();
            col.Item().Text(s.Property.FullAddress).FontColor(Muted);
        });
        row.ConstantItem(170).AlignRight().Column(col =>
        {
            col.Item().AlignRight().Text(text.Format(text.ReportNumberFormat, s.ReportNumber)).Bold();
            col.Item().AlignRight().Text(text.Format(text.VersionFormat, s.VersionNumber)).FontColor(Muted);
            col.Item().AlignRight().Text(text.Format(text.GeneratedFormat, text.ShortDateTime(s.GeneratedAt))).FontColor(Muted);
        });
    });

    private static void Footer(IContainer c, ReportSnapshot s, ReportPdfText text) => c.BorderTop(0.5f).BorderColor(Border).PaddingTop(4).Row(row =>
    {
        row.RelativeItem().Text(text.Format(text.FooterReportIdFormat, s.ReportNumber, s.ReportId)).FontSize(7.5f).FontColor(Muted);
        row.ConstantItem(90).AlignRight().Text(t =>
        {
            t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Muted));
            t.Span(text.Page);
            t.CurrentPageNumber();
            t.Span(text.PageOf);
            t.TotalPages();
        });
    });

    private static List<(string, string)> SummaryRows(ReportSnapshot s, ReportPdfText text)
    {
        var rows = new List<(string, string)>
        {
            (text.Property, s.Property.FullAddress),
            (text.PropertyType, text.Label(s.Property.PropertyType)),
            (text.InspectionType, text.Label(s.Inspection.Type)),
            (text.InspectionDate, text.LongDate(s.Inspection.CompletedAt)),
            (text.Inspector, s.Agent?.Name ?? "—"),
            (text.Tenants, s.Tenants.Count == 0 ? "—" : string.Join(", ", s.Tenants.Select(t => t.Name))),
        };
        if (s.Inspection.TenancyStartDate is { } start)
            rows.Add((text.Tenancy, $"{text.ShortDate(start)} – {(s.Inspection.TenancyEndDate is { } end ? text.ShortDate(end) : text.Ongoing)}"));
        if (s.Inspection.ComparisonReportNumber is not null)
        {
            var comparedOn = s.Inspection.ComparisonCompletedAt is { } at ? text.ShortDate(at) : "";
            rows.Add((text.ComparedWith, $"{text.Format(text.ReportNumberFormat, s.Inspection.ComparisonReportNumber)} ({comparedOn})"));
        }
        rows.Add((text.RoomsInspected, s.Rooms.Count.ToString(Culture)));
        return rows;
    }

    private static void Summary(IContainer c, ReportSnapshot s, ReportPdfText text) => c.Border(0.5f).BorderColor(Border).Padding(8).Table(t =>
    {
        t.ColumnsDefinition(cols => { cols.ConstantColumn(110); cols.RelativeColumn(); });
        foreach (var (label, value) in SummaryRows(s, text))
        {
            t.Cell().PaddingVertical(2).Text(label).FontColor(Muted);
            t.Cell().PaddingVertical(2).Text(value).SemiBold();
        }
    });

    private static void Disclaimer(IContainer c, ReportPdfText text) =>
        c.Background("#F3F6F7").Padding(8).Text(text.Disclaimer).FontSize(8).FontColor(Muted).Italic();

    private static void ComparisonSummary(IContainer c, ReportComparisonSummary summary, ReportPdfText text) => c.Column(col =>
    {
        col.Item().Text(text.ComparisonSummaryTitle).FontSize(12).Bold().FontColor(Primary);
        var counts = string.Join(", ", summary.DecisionCounts.Select(kv => $"{text.Label(kv.Key)}: {kv.Value}"));
        col.Item().Text(text.Format(text.ComparisonSummaryFormat, summary.SourceReportNumber ?? "—", summary.RoomsCompared, counts)).FontColor(Muted);
        col.Item().PaddingTop(4).Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(5); });
            t.Header(h =>
            {
                foreach (var title in new[] { text.Room, text.InspectorDecision, text.Notes })
                    h.Cell().Background(Primary).Padding(4).Text(title).FontColor(Colors.White).SemiBold();
            });
            foreach (var item in summary.Items)
            {
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(item.RoomName);
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(text.Label(item.Decision)).SemiBold();
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(Clean(item.Notes) ?? "—");
            }
        });
    });

    private static void Room(IContainer c, ReportRoom room, IReadOnlyDictionary<string, byte[]> images, ReportPdfText text) => c.Column(col =>
    {
        col.Spacing(6);
        col.Item().Background("#E8F0F2").Padding(6).Row(r =>
        {
            r.RelativeItem().Text($"{room.Sequence}. {room.Name}").FontSize(12).Bold().FontColor(Primary);
            r.ConstantItem(140).AlignRight().Text(text.Label(room.RoomType)).FontColor(Muted);
        });
        col.Item().Text(t =>
        {
            t.Span(text.Condition).SemiBold();
            t.Span(Clean(room.Description) ?? text.NoDescription);
        });
        if (room.Photos.Count > 0) col.Item().Element(x => Photos(x, room.Photos, images, text));
        Defects(col, room, images, text);
        if (!string.IsNullOrWhiteSpace(room.AgentNotes))
            col.Item().Text(t => { t.Span(text.InspectorNotes).SemiBold(); t.Span(Clean(room.AgentNotes)!); });
        if (room.Comparison is { } cmp) col.Item().Element(x => RoomComparison(x, cmp, text));
    });

    private static void Defects(ColumnDescriptor col, ReportRoom room, IReadOnlyDictionary<string, byte[]> images, ReportPdfText text)
    {
        if (room.Defects.Count == 0)
        {
            col.Item().Text(text.NoDefects).FontColor(Muted);
            return;
        }
        col.Item().PaddingTop(4).Text(text.Format(text.DefectsFormat, room.Defects.Count)).SemiBold().FontColor("#9A3412");
        var index = 1;
        foreach (var d in room.Defects)
        {
            var number = index++;
            col.Item().BorderLeft(2).BorderColor("#F59E0B").PaddingLeft(8).Column(dc =>
            {
                dc.Spacing(3);
                dc.Item().Text(t =>
                {
                    t.Span($"{number}. {Clean(d.Title) ?? text.Defect}").SemiBold();
                    if (!string.IsNullOrWhiteSpace(d.Location)) t.Span($" — {d.Location}").FontColor(Muted);
                    t.Span($"  [{text.Label(d.Classification)}]").FontColor(Muted);
                });
                dc.Item().Text(Clean(d.Description) ?? "—");
                if (d.Photos.Count > 0) dc.Item().Element(x => Photos(x, d.Photos, images, text, 120));
            });
        }
    }

    private static void RoomComparison(IContainer c, ReportRoomComparison cmp, ReportPdfText text) => c.Border(0.5f).BorderColor(Border).Padding(6).Column(cc =>
    {
        cc.Spacing(2);
        cc.Item().Text(text.ComparisonWithMoveIn).SemiBold().FontColor(Primary);
        cc.Item().Text(t => { t.Span(text.MoveInRecord).FontColor(Muted); t.Span(Clean(cmp.BaselineDescription) ?? "—"); });
        if (cmp.BaselineDefects.Count > 0)
            cc.Item().Text(t => { t.Span(text.MoveInDefects).FontColor(Muted); t.Span(string.Join("; ", cmp.BaselineDefects.Select(x => Clean(x)))); });
        cc.Item().Text(t => { t.Span(text.InspectorDecision + ": ").FontColor(Muted); t.Span(text.Label(cmp.Decision ?? "Pending")).Bold(); });
        if (!string.IsNullOrWhiteSpace(cmp.Notes))
            cc.Item().Text(t => { t.Span(text.Notes + ": ").FontColor(Muted); t.Span(Clean(cmp.Notes)!); });
    });

    private static void Photos(IContainer c, IReadOnlyList<ReportPhoto> photos, IReadOnlyDictionary<string, byte[]> images, ReportPdfText text, float height = 165) =>
        c.Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
            foreach (var photo in photos)
            {
                t.Cell().Padding(2).Height(height).Border(0.5f).BorderColor(Border).AlignCenter().AlignMiddle().Element(cell =>
                {
                    if (images.TryGetValue(photo.StorageKey, out var bytes))
                    {
                        try
                        {
                            cell.Image(bytes).FitArea().WithRasterDpi(144).WithCompressionQuality(ImageCompressionQuality.Medium);
                            return;
                        }
                        catch (Exception)
                        {
                            // Fall through to the placeholder for undecodable images.
                        }
                    }
                    cell.Text(text.PhotoUnavailable).FontColor(Muted).FontSize(8);
                });
            }
        });

    /// <summary>Normalise glyphs the embedded font may lack.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace("→", "->", StringComparison.Ordinal).Trim();
}

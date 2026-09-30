using System.Globalization;
using InspectFlow.Modules.Reports.Application;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>Renders the immutable report snapshot to a professional A4 PDF.</summary>
public sealed class QuestPdfReportService : IPdfService
{
    private const string Primary = "#0F4C5C";
    private const string Muted = "#5B6770";
    private const string Border = "#D9DEE2";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public byte[] RenderInspectionReport(ReportSnapshot snapshot, IReadOnlyDictionary<string, byte[]> images)
    {
        var s = snapshot;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor("#1F2A30"));
            page.Header().Element(c => Header(c, s));
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);
                col.Item().Element(c => Summary(c, s));
                col.Item().Element(Disclaimer);
                if (s.Comparison is not null) col.Item().Element(c => ComparisonSummary(c, s.Comparison));
                foreach (var room in s.Rooms.OrderBy(r => r.Sequence))
                    col.Item().Element(c => Room(c, room, images));
            });
            page.Footer().Element(c => Footer(c, s));
        }))
        .WithMetadata(new DocumentMetadata
        {
            Title = $"Inspection report {s.ReportNumber}",
            Author = s.Company.Name,
            Subject = $"{Humanize(s.Inspection.Type)} inspection - {s.Property.FullAddress}",
            Creator = "InspectFlow",
            CreationDate = s.GeneratedAt,
            ModifiedDate = s.GeneratedAt,
        })
        .GeneratePdf();
    }

    private static void Header(IContainer c, ReportSnapshot s) => c.BorderBottom(1).BorderColor(Primary).PaddingBottom(8).Row(row =>
    {
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(s.Company.Name).FontSize(15).Bold().FontColor(Primary);
            col.Item().Text($"{Humanize(s.Inspection.Type)} Inspection Report").FontSize(11).SemiBold();
            col.Item().Text(s.Property.FullAddress).FontColor(Muted);
        });
        row.ConstantItem(170).AlignRight().Column(col =>
        {
            col.Item().AlignRight().Text($"Report {s.ReportNumber}").Bold();
            col.Item().AlignRight().Text($"Version {s.VersionNumber}").FontColor(Muted);
            col.Item().AlignRight().Text($"Generated {s.GeneratedAt.ToString("d MMM yyyy HH:mm", Culture)} UTC").FontColor(Muted);
        });
    });

    private static void Footer(IContainer c, ReportSnapshot s) => c.BorderTop(0.5f).BorderColor(Border).PaddingTop(4).Row(row =>
    {
        row.RelativeItem().Text($"{s.ReportNumber} · Report ID {s.ReportId}").FontSize(7.5f).FontColor(Muted);
        row.ConstantItem(90).AlignRight().Text(t =>
        {
            t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Muted));
            t.Span("Page ");
            t.CurrentPageNumber();
            t.Span(" of ");
            t.TotalPages();
        });
    });

    private static void Summary(IContainer c, ReportSnapshot s)
    {
        var rows = new List<(string, string)>
        {
            ("Property", s.Property.FullAddress),
            ("Property type", Humanize(s.Property.PropertyType)),
            ("Inspection type", Humanize(s.Inspection.Type)),
            ("Inspection date", s.Inspection.CompletedAt.ToString("d MMMM yyyy", Culture)),
            ("Inspector", s.Agent?.Name ?? "—"),
            ("Tenant(s)", s.Tenants.Count == 0 ? "—" : string.Join(", ", s.Tenants.Select(t => t.Name))),
        };
        if (s.Inspection.TenancyStartDate is { } start)
            rows.Add(("Tenancy", $"{start.ToString("d MMM yyyy", Culture)} – {s.Inspection.TenancyEndDate?.ToString("d MMM yyyy", Culture) ?? "ongoing"}"));
        if (s.Inspection.ComparisonReportNumber is not null)
            rows.Add(("Compared with", $"Report {s.Inspection.ComparisonReportNumber} ({s.Inspection.ComparisonCompletedAt?.ToString("d MMM yyyy", Culture)})"));
        rows.Add(("Rooms inspected", s.Rooms.Count.ToString(Culture)));

        c.Border(0.5f).BorderColor(Border).Padding(8).Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.ConstantColumn(110); cols.RelativeColumn(); });
            foreach (var (label, value) in rows)
            {
                t.Cell().PaddingVertical(2).Text(label).FontColor(Muted);
                t.Cell().PaddingVertical(2).Text(value).SemiBold();
            }
        });
    }

    private static void Disclaimer(IContainer c) => c.Background("#F3F6F7").Padding(8).Text(
            "This report records the condition of the property that was visible at the time of the inspection. " +
            "Descriptions may have been drafted with AI assistance and were reviewed and confirmed by the inspector. " +
            "Areas not shown or not accessible were not assessed. This report does not determine responsibility or liability for any damage.")
        .FontSize(8).FontColor(Muted).Italic();

    private static void ComparisonSummary(IContainer c, ReportComparisonSummary summary) => c.Column(col =>
    {
        col.Item().Text("Move In / Move Out comparison summary").FontSize(12).Bold().FontColor(Primary);
        col.Item().Text($"Compared with report {summary.SourceReportNumber ?? "—"} · {summary.RoomsCompared} room(s) · " +
                        string.Join(", ", summary.DecisionCounts.Select(kv => $"{Humanize(kv.Key)}: {kv.Value}"))).FontColor(Muted);
        col.Item().PaddingTop(4).Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(5); });
            t.Header(h =>
            {
                foreach (var title in new[] { "Room", "Inspector decision", "Notes" })
                    h.Cell().Background(Primary).Padding(4).Text(title).FontColor(Colors.White).SemiBold();
            });
            foreach (var item in summary.Items)
            {
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(item.RoomName);
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(Humanize(item.Decision)).SemiBold();
                t.Cell().BorderBottom(0.5f).BorderColor(Border).Padding(4).Text(Clean(item.Notes) ?? "—");
            }
        });
    });

    private static void Room(IContainer c, ReportRoom room, IReadOnlyDictionary<string, byte[]> images) => c.Column(col =>
    {
        col.Spacing(6);
        col.Item().Background("#E8F0F2").Padding(6).Row(r =>
        {
            r.RelativeItem().Text($"{room.Sequence}. {room.Name}").FontSize(12).Bold().FontColor(Primary);
            r.ConstantItem(140).AlignRight().Text(Humanize(room.RoomType)).FontColor(Muted);
        });
        col.Item().Text(t =>
        {
            t.Span("Condition: ").SemiBold();
            t.Span(Clean(room.Description) ?? "No description recorded.");
        });
        if (room.Photos.Count > 0) col.Item().Element(x => Photos(x, room.Photos, images));

        if (room.Defects.Count > 0)
        {
            col.Item().PaddingTop(4).Text($"Defects ({room.Defects.Count})").SemiBold().FontColor("#9A3412");
            var i = 1;
            foreach (var d in room.Defects)
            {
                var index = i++;
                col.Item().BorderLeft(2).BorderColor("#F59E0B").PaddingLeft(8).Column(dc =>
                {
                    dc.Spacing(3);
                    dc.Item().Text(t =>
                    {
                        t.Span($"{index}. {Clean(d.Title) ?? "Defect"}").SemiBold();
                        if (!string.IsNullOrWhiteSpace(d.Location)) t.Span($" — {d.Location}").FontColor(Muted);
                        t.Span($"  [{Humanize(d.Classification)}]").FontColor(Muted);
                    });
                    dc.Item().Text(Clean(d.Description) ?? "—");
                    if (d.Photos.Count > 0) dc.Item().Element(x => Photos(x, d.Photos, images, 120));
                });
            }
        }
        else
        {
            col.Item().Text("No defects recorded.").FontColor(Muted);
        }

        if (!string.IsNullOrWhiteSpace(room.AgentNotes))
            col.Item().Text(t => { t.Span("Inspector notes: ").SemiBold(); t.Span(Clean(room.AgentNotes)!); });

        if (room.Comparison is { } cmp)
        {
            col.Item().Border(0.5f).BorderColor(Border).Padding(6).Column(cc =>
            {
                cc.Spacing(2);
                cc.Item().Text("Comparison with Move In").SemiBold().FontColor(Primary);
                cc.Item().Text(t => { t.Span("Move In record: ").FontColor(Muted); t.Span(Clean(cmp.BaselineDescription) ?? "—"); });
                if (cmp.BaselineDefects.Count > 0)
                    cc.Item().Text(t => { t.Span("Move In defects: ").FontColor(Muted); t.Span(string.Join("; ", cmp.BaselineDefects.Select(x => Clean(x)))); });
                cc.Item().Text(t => { t.Span("Inspector decision: ").FontColor(Muted); t.Span(Humanize(cmp.Decision ?? "Pending")).Bold(); });
                if (!string.IsNullOrWhiteSpace(cmp.Notes))
                    cc.Item().Text(t => { t.Span("Notes: ").FontColor(Muted); t.Span(Clean(cmp.Notes)!); });
            });
        }
    });

    private static void Photos(IContainer c, IReadOnlyList<ReportPhoto> photos, IReadOnlyDictionary<string, byte[]> images, float height = 165) =>
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
                    cell.Text("Photo unavailable").FontColor(Muted).FontSize(8);
                });
            }
        });

    private static string Humanize(string value) => value switch
    {
        "MoveIn" => "Move In",
        "MoveOut" => "Move Out",
        "LivingRoom" => "Living Room",
        "DiningRoom" => "Dining Room",
        "NewDamage" => "New damage",
        "PreExisting" => "Pre-existing",
        "NormalWear" => "Normal wear",
        "UnableToDetermine" => "Unable to determine",
        _ => value,
    };

    /// <summary>Normalise glyphs the embedded font may lack.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace("→", "->", StringComparison.Ordinal).Trim();
}

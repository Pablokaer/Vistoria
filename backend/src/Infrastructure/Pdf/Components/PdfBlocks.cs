using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static InspectFlow.Infrastructure.Pdf.PdfTheme;

namespace InspectFlow.Infrastructure.Pdf.Components;

/// <summary>Small building blocks shared by the report sections.</summary>
internal static class PdfBlocks
{
    /// <summary>Uppercase label with an optional icon, e.g. "CONDITION". Distinguishes a label from the content under it.</summary>
    public static void Label(IContainer c, string text, string? icon = null, string color = Muted) => c.Row(row =>
    {
        if (icon is not null) row.AutoItem().PaddingRight(Space1).AlignMiddle().Element(x => PdfIcons.Sized(x, icon, color, 9));
        row.AutoItem().AlignMiddle().Text(text.ToUpperInvariant()).FontSize(LabelSize).SemiBold().LetterSpacing(0.06f).FontColor(color);
    });

    public enum Tone { Success, Warning, Danger, Neutral, Brand }

    private static (string Fg, string Bg) Colours(Tone tone) => tone switch
    {
        Tone.Success => (Success, SuccessSoft),
        Tone.Warning => (Warning, WarningSoft),
        Tone.Danger => (Danger, DangerSoft),
        Tone.Brand => (Brand, BrandSoft),
        _ => (Ink2, Surface),
    };

    /// <summary>Status pill with icon + label (never colour alone).</summary>
    public static void Pill(IContainer c, string text, Tone tone, string? icon = null)
    {
        var (fg, bg) = Colours(tone);
        c.Background(bg).CornerRadius(Radius).PaddingVertical(2.5f).PaddingHorizontal(Space2).Row(row =>
        {
            if (icon is not null) row.AutoItem().PaddingRight(Space1).AlignMiddle().Element(x => PdfIcons.Sized(x, icon, fg, 8.5f));
            row.AutoItem().AlignMiddle().Text(text).FontSize(MetaSize - 0.5f).SemiBold().FontColor(fg);
        });
    }

    /// <summary>Tone of a defect classification or comparison decision: new damage stands out, the rest stays calm.</summary>
    public static Tone ToneOf(string classification) => classification switch
    {
        "NewDamage" => Tone.Danger,
        "NormalWear" or "Unknown" or "UnableToDetermine" or "Pending" => Tone.Warning,
        "Unchanged" or "Resolved" => Tone.Success,
        _ => Tone.Neutral,
    };

    /// <summary>Discreet information callout (light blue, info icon, smaller type).</summary>
    public static void Callout(IContainer c, string title, string body) =>
        c.Background(BrandSoft).CornerRadius(Radius).Padding(PanelPadding).Row(row =>
        {
            row.AutoItem().PaddingRight(Space3).Element(x => PdfIcons.Sized(x, PdfIcons.Info, Brand, 13));
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(title).FontSize(MetaSize).SemiBold().FontColor(PdfTheme.Navy);
                col.Item().PaddingTop(Space1).Text(body).FontSize(MetaSize - 0.5f).FontColor(Ink2).LineHeight(1.35f);
            });
        });

    /// <summary>Label above value (used in grids of facts).</summary>
    public static void Field(IContainer c, string label, string value, string? icon = null) => c.Row(row =>
    {
        if (icon is not null) row.AutoItem().PaddingRight(Space2).PaddingTop(1).Element(x => PdfIcons.Sized(x, icon, Faint, 12));
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(label).FontSize(LabelSize).FontColor(Muted);
            col.Item().Text(value).FontSize(BodySize).SemiBold().FontColor(Ink);
        });
    });

    /// <summary>Body paragraph; empty values render as a muted placeholder sentence, never as blank space.</summary>
    public static void Paragraph(IContainer c, string? value, string whenEmpty) =>
        c.Text(Clean(value) ?? whenEmpty).Style(Body).FontColor(Clean(value) is null ? Muted : Ink2).Italic(Clean(value) is null);

    /// <summary>Normalise glyphs the embedded font may lack.</summary>
    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace("→", "->", StringComparison.Ordinal).Trim();
}

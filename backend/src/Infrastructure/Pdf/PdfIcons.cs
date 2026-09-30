using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>Line icons for the report (same stroke style as the web app's icons.tsx), rendered as SVG.</summary>
internal static class PdfIcons
{
    public const string Building = "<path d='M4 21V5a1 1 0 0 1 1-1h9a1 1 0 0 1 1 1v16'/><path d='M15 9h4a1 1 0 0 1 1 1v11'/><path d='M8 8h3M8 12h3M8 16h3M3 21h18'/>";
    public const string Clipboard = "<rect x='5' y='4' width='14' height='17' rx='1.5'/><path d='M9 4V3h6v1M9 11h6M9 15h4'/>";
    public const string Calendar = "<rect x='3.5' y='5' width='17' height='15.5' rx='1.5'/><path d='M3.5 10h17M8 3v4M16 3v4'/>";
    public const string Rooms = "<rect x='3' y='3' width='18' height='18' rx='1.5'/><path d='M3 12h9V3M12 12v9M12 16h9'/>";
    public const string User = "<circle cx='12' cy='8' r='4'/><path d='M4.5 21a7.5 7.5 0 0 1 15 0'/>";
    public const string Users = "<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20a6.5 6.5 0 0 1 13 0'/><path d='M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.2a6.5 6.5 0 0 1 3.5 5.8'/>";
    public const string Key = "<circle cx='8' cy='15' r='4'/><path d='m11 12 9-9M16 7l2.5 2.5M18.5 4.5 21 7'/>";
    public const string Compare = "<rect x='3' y='5' width='7' height='14' rx='1'/><rect x='14' y='5' width='7' height='14' rx='1'/><path d='M10 12h4'/>";
    public const string Info = "<circle cx='12' cy='12' r='9'/><path d='M12 11v5.5M12 7.5h.01'/>";
    public const string Check = "<circle cx='12' cy='12' r='9'/><path d='m8 12.5 2.8 2.8L16.5 9.5'/>";
    public const string Alert = "<path d='M12 4 2.5 20h19Z'/><path d='M12 10v4M12 17h.01'/>";
    public const string Camera = "<path d='M4 8a2 2 0 0 1 2-2h1.5l1.5-2h6l1.5 2H18a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2Z'/><circle cx='12' cy='12.5' r='3.5'/>";
    public const string Notes = "<path d='M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16Z'/><path d='m13.5 6.5 4 4'/>";
    public const string Shield = "<path d='M12 3 5 6v5c0 4.5 3 8.5 7 10 4-1.5 7-5.5 7-10V6Z'/><path d='m9 12 2 2 4-4'/>";

    /// <summary>The InspectFlow mark: a roofline over a check, white on a rounded brand-blue square.</summary>
    private const string Mark =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'><rect width='32' height='32' rx='7' fill='" + PdfTheme.Brand + "'/>" +
        "<g transform='translate(4 4)' fill='none' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<path d='M3.5 11 12 4.5l8.5 6.5'/><path d='M6 9.5V19.5h12V9.5'/><path d='m9 14.2 2.1 2.1L15.2 12'/></g></svg>";

    /// <summary>Draws an icon in the given colour. Example: <c>c.Width(10).Height(10).Element(x =&gt; PdfIcons.Draw(x, PdfIcons.Calendar, PdfTheme.Muted))</c>.</summary>
    public static void Draw(IContainer container, string paths, string color) =>
        container.Svg($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='{color}' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'>{paths}</svg>");

    /// <summary>Icon of a fixed square size.</summary>
    public static void Sized(IContainer container, string paths, string color, float size) =>
        container.Width(size).Height(size).Element(x => Draw(x, paths, color));

    /// <summary>Logo: mark + "InspectFlow" wordmark. <paramref name="markSize"/> sets the scale.</summary>
    public static void Logo(IContainer container, float markSize) => container.Row(row =>
    {
        row.AutoItem().Width(markSize).Height(markSize).Svg(Mark);
        row.AutoItem().PaddingLeft(markSize * 0.3f).AlignMiddle().Text("InspectFlow")
            .FontSize(markSize * 0.55f).Bold().FontColor(PdfTheme.Navy);
    });
}

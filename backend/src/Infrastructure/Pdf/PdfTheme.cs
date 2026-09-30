using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Design tokens of the report PDF — the print counterpart of frontend/src/app/globals.css (ADR 0015). Every
/// size, colour and gap in the report comes from here; components never use magic numbers.
/// </summary>
internal static class PdfTheme
{
    // Colours: white page, deep navy identity, brand blue accents, neutral greys, semantic tones used sparingly.
    public const string Navy = "#0F1B33";
    public const string Brand = "#1D4ED8";
    public const string BrandSoft = "#EFF5FF";
    public const string BrandLine = "#BFD3FE";
    public const string Ink = "#0F172A";
    public const string Ink2 = "#334155";
    public const string Muted = "#64748B";
    public const string Faint = "#94A3B8";
    public const string Line = "#E2E6EC";
    public const string Surface = "#F5F7FA";
    public const string Success = "#067647";
    public const string SuccessSoft = "#ECFDF3";
    public const string Warning = "#B54708";
    public const string WarningSoft = "#FFFAEB";
    public const string Danger = "#B42318";
    public const string DangerSoft = "#FEF3F2";
    public const string MoveIn = "#0E9384";
    public const string MoveOut = "#C4320A";
    public const string Periodic = "#444CE7";

    // Page geometry (A4 = 595 × 842 pt).
    public const float PageMarginX = 40;
    public const float PageMarginY = 30;
    public const float ContentWidth = 595 - 2 * PageMarginX;

    // Spacing scale.
    public const float Space1 = 3;
    public const float Space2 = 6;
    public const float Space3 = 10;
    public const float Space4 = 14;
    public const float Space5 = 20;
    public const float Space6 = 28;
    public const float PhotoGap = 6;
    public const float PanelPadding = 12;
    public const float Radius = 4;

    // Type scale (pt). Body stays ≥ 9.5 so the report reads well printed on A4.
    public const float TitleSize = 30;
    public const float EyebrowSize = 11;
    public const float AddressSize = 13;
    public const float RoomTitleSize = 15;
    public const float SectionSize = 11;
    public const float LabelSize = 7.5f;
    public const float BodySize = 9.5f;
    public const float MetaSize = 8.5f;
    public const float CaptionSize = 7;
    public const float MetricSize = 20;
    public const float BodyLineHeight = 1.4f;

    // Photo cell heights, chosen from the content width so typical 4:3 photos fill their cells (contain, never stretched).
    public const float PhotoSingle = 240;
    public const float PhotoHalf = 172;
    public const float PhotoFeature = 214;
    public const float PhotoThird = 118;
    public const float PhotoDefect = 128;
    public const float PhotoBeside = 205;
    public const float PhotoComparison = 140;

    // Keep-together thresholds: a block that needs at least this much space starts on a new page instead.
    public const float RoomMinSpace = 250;
    public const float TextMinSpace = 70;
    public const float DefectMinSpace = 110;
    /// <summary>Texts up to this length are kept on the same page as their photos (longer ones may flow on).</summary>
    public const int KeepWithPhotosMaxChars = 900;
    /// <summary>Up to this many rows in the Move In comparison table, the whole summary is kept on one page.</summary>
    public const int SummaryKeepTogetherMaxRooms = 12;

    /// <summary>Lato is bundled with QuestPDF, so the report renders identically in Docker and on developer machines.</summary>
    public const string FontFamily = "Lato";

    public static TextStyle Body => TextStyle.Default.FontSize(BodySize).FontColor(Ink2).LineHeight(BodyLineHeight);
}

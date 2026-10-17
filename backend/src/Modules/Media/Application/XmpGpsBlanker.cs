using System.Text;
using System.Text.RegularExpressions;

namespace InspectFlow.Modules.Media.Application;

/// <summary>
/// Blanks the GPS values of an XMP packet in place (<c>exif:GPSLatitude="53,20.75N"</c> or
/// <c>&lt;exif:GPSLatitude&gt;53,20.75N&lt;/exif:GPSLatitude&gt;</c>): each value character becomes a space, so the
/// packet keeps its length and the container around it stays valid. Other XMP properties are not touched.
/// Example: <c>XmpGpsBlanker.Blank(packet) // true when a coordinate was blanked</c>
/// </summary>
internal static partial class XmpGpsBlanker
{
    private const int TimeoutMilliseconds = 250;

    public static bool Blank(Span<byte> packet)
    {
        // Latin-1 maps every byte to exactly one char, so match indexes are byte offsets in the UTF-8 packet.
        var text = Encoding.Latin1.GetString(packet);
        try
        {
            return BlankMatches(packet, AttributeValues().Matches(text)) | BlankMatches(packet, ElementValues().Matches(text));
        }
        catch (RegexMatchTimeoutException)
        {
            return false; // a pathological packet is left as it is rather than failing the upload
        }
    }

    private static bool BlankMatches(Span<byte> packet, MatchCollection matches)
    {
        var blanked = false;
        foreach (Match match in matches)
        {
            var value = match.Groups["value"];
            blanked |= !string.IsNullOrWhiteSpace(value.Value);
            packet.Slice(value.Index, value.Length).Fill((byte)' ');
        }
        return blanked;
    }

    // Any namespace prefix: the "exif" prefix is a convention, not a rule.
    [GeneratedRegex("""\b[A-Za-z][\w-]*:GPS\w*\s*=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)')""", RegexOptions.CultureInvariant, TimeoutMilliseconds)]
    private static partial Regex AttributeValues();

    [GeneratedRegex("""<(?<name>[A-Za-z][\w-]*:GPS\w*)\b[^<>]*(?<!/)>(?<value>[^<]*)</\k<name>\s*>""", RegexOptions.CultureInvariant, TimeoutMilliseconds)]
    private static partial Regex ElementValues();
}

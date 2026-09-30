using InspectFlow.Modules.Reports.Application;
using QuestPDF.Infrastructure;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>
/// Decodes each photo once per document (the same photo can appear in a room and in its comparison). Photos that
/// are missing or cannot be decoded resolve to null and render as a discreet placeholder; the evidence bytes are
/// never altered.
/// </summary>
internal sealed class ReportImages(IReadOnlyDictionary<string, byte[]> bytesByKey)
{
    private readonly Dictionary<string, Image?> _decoded = new(StringComparer.Ordinal);

    /// <summary>Example: <c>images.Find(photo) is { } image ? cell.Image(image).FitArea() : placeholder</c>.</summary>
    public Image? Find(ReportPhoto photo)
    {
        if (_decoded.TryGetValue(photo.StorageKey, out var cached)) return cached;
        var image = Decode(photo.StorageKey);
        _decoded[photo.StorageKey] = image;
        return image;
    }

    private Image? Decode(string key)
    {
        if (!bytesByKey.TryGetValue(key, out var bytes)) return null;
        try
        {
            return Image.FromBinaryData(bytes);
        }
        catch (Exception)
        {
            // Undecodable upload (content sniffing lets through a corrupt file): show the placeholder instead.
            return null;
        }
    }
}

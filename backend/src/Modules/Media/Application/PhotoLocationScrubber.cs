using System.Buffers.Binary;
using System.Text;

namespace InspectFlow.Modules.Media.Application;

/// <summary>
/// Removes where a photo was taken before it is stored: the EXIF GPS block (JPEG APP1, PNG eXIf, WebP EXIF) and the
/// GPS values of XMP packets (JPEG APP1, PNG iTXt, WebP "XMP "). Everything else stays as uploaded — orientation (so
/// the photo is not shown rotated), date taken and camera (evidence in a dispute) — and the file is edited in place
/// without re-encoding, so its length and pixels never change. Unknown or malformed structures are left as they are.
/// Example: <c>PhotoLocationScrubber.RemoveLocation(bytes, "image/jpeg") // true when coordinates were removed</c>
/// </summary>
public static class PhotoLocationScrubber
{
    private const byte JpegApp1 = 0xE1;
    private static readonly uint PngExif = FourCc("eXIf");
    private static readonly uint PngInternationalText = FourCc("iTXt");
    private static readonly uint WebpExif = FourCc("EXIF");
    private static readonly uint WebpXmp = FourCc("XMP ");

    private static ReadOnlySpan<byte> JpegStart => [0xFF, 0xD8];
    private static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;
    private static ReadOnlySpan<byte> XmpHeader => "http://ns.adobe.com/xap/1.0/\0"u8;
    private static ReadOnlySpan<byte> ExtendedXmpHeader => "http://ns.adobe.com/xmp/extension/\0"u8;
    private static ReadOnlySpan<byte> PngXmpKeyword => "XML:com.adobe.xmp\0"u8;
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <param name="mimeType">The detected type (<see cref="ImageFileValidator.DetectMimeType"/>), not the declared one.</param>
    /// <returns>True when GPS data was found and removed.</returns>
    public static bool RemoveLocation(Span<byte> image, string mimeType) => mimeType switch
    {
        "image/jpeg" => image.StartsWith(JpegStart) && ScrubChunks(image, JpegStart.Length, TryReadJpegSegment, ScrubJpegSegment),
        "image/png" => image.StartsWith(PngSignature) && ScrubChunks(image, PngSignature.Length, TryReadPngChunk, ScrubPngChunk),
        "image/webp" => IsWebp(image) && ScrubChunks(image, 12, TryReadWebpChunk, ScrubWebpChunk),
        _ => false,
    };

    /// <summary>One metadata container unit: a JPEG segment, PNG chunk or WebP chunk, with its payload range.</summary>
    private readonly record struct ContainerChunk(uint Tag, int Start, int Length, int Next);

    private delegate bool ChunkReader(ReadOnlySpan<byte> image, int position, out ContainerChunk chunk);

    private delegate bool ChunkScrubber(Span<byte> image, ContainerChunk chunk);

    private static bool ScrubChunks(Span<byte> image, int start, ChunkReader read, ChunkScrubber scrub)
    {
        var removed = false;
        for (var position = start; read(image, position, out var chunk); position = chunk.Next)
            removed |= scrub(image, chunk);
        return removed;
    }

    // ---------- JPEG: FF marker, 2-byte big-endian length (includes itself), payload; stops at SOS (entropy data follows) ----------

    private static bool TryReadJpegSegment(ReadOnlySpan<byte> image, int position, out ContainerChunk chunk)
    {
        chunk = default;
        var markerAt = SkipFillBytes(image, position + 1);
        if (position >= image.Length || image[position] != 0xFF || markerAt + 3 > image.Length) return false;
        var marker = image[markerAt];
        if (!HasLength(marker)) return false;
        var length = BinaryPrimitives.ReadUInt16BigEndian(image.Slice(markerAt + 1, 2));
        if (length < 2 || markerAt + 1 + length > image.Length) return false;
        chunk = new ContainerChunk(marker, markerAt + 3, length - 2, markerAt + 1 + length);
        return true;
    }

    private static int SkipFillBytes(ReadOnlySpan<byte> image, int markerAt)
    {
        while (markerAt < image.Length && image[markerAt] == 0xFF) markerAt++;
        return markerAt;
    }

    // SOS (DA), EOI (D9), SOI (D8), RSTn (D0-D7) and TEM (01) carry no length; reading past SOS would walk image data.
    private static bool HasLength(byte marker) => marker is not (0xDA or 0x01) && marker is not (>= 0xD0 and <= 0xD9);

    private static bool ScrubJpegSegment(Span<byte> image, ContainerChunk chunk)
    {
        if (chunk.Tag != JpegApp1) return false;
        var payload = image.Slice(chunk.Start, chunk.Length);
        if (payload.StartsWith(ExifHeader)) return ExifGpsEraser.Erase(payload[ExifHeader.Length..]);
        return (payload.StartsWith(XmpHeader) || payload.StartsWith(ExtendedXmpHeader)) && XmpGpsBlanker.Blank(payload);
    }

    // ---------- PNG: 4-byte big-endian length, type, data, CRC-32 over type + data ----------

    private static bool TryReadPngChunk(ReadOnlySpan<byte> image, int position, out ContainerChunk chunk)
    {
        chunk = default;
        if (position < 0 || position + 12 > image.Length) return false;
        var length = BinaryPrimitives.ReadUInt32BigEndian(image.Slice(position, 4));
        if (length > (uint)(image.Length - position - 12)) return false;
        var tag = BinaryPrimitives.ReadUInt32BigEndian(image.Slice(position + 4, 4));
        chunk = new ContainerChunk(tag, position + 8, (int)length, position + 12 + (int)length);
        return true;
    }

    private static bool ScrubPngChunk(Span<byte> image, ContainerChunk chunk)
    {
        var data = image.Slice(chunk.Start, chunk.Length);
        var changed = chunk.Tag == PngExif ? ExifGpsEraser.Erase(data)
            : chunk.Tag == PngInternationalText && IsUncompressedXmp(data) && XmpGpsBlanker.Blank(data);
        if (!changed) return false;
        var crc = PngCrc32.Compute(image.Slice(chunk.Start - 4, chunk.Length + 4));
        BinaryPrimitives.WriteUInt32BigEndian(image.Slice(chunk.Start + chunk.Length, 4), crc);
        return true;
    }

    // iTXt: keyword NUL, compression flag, compression method, ... — a compressed packet cannot be blanked in place.
    private static bool IsUncompressedXmp(ReadOnlySpan<byte> data) =>
        data.StartsWith(PngXmpKeyword) && data.Length > PngXmpKeyword.Length && data[PngXmpKeyword.Length] == 0;

    // ---------- WebP: "RIFF" size "WEBP", then chunks: FourCC, 4-byte little-endian size, data padded to even ----------

    private static bool IsWebp(ReadOnlySpan<byte> image) =>
        image.Length >= 12 && image.StartsWith("RIFF"u8) && image.Slice(8, 4).SequenceEqual("WEBP"u8);

    private static bool TryReadWebpChunk(ReadOnlySpan<byte> image, int position, out ContainerChunk chunk)
    {
        chunk = default;
        if (position < 0 || position + 8 > image.Length) return false;
        var size = BinaryPrimitives.ReadUInt32LittleEndian(image.Slice(position + 4, 4));
        if (size > (uint)(image.Length - position - 8)) return false;
        var tag = BinaryPrimitives.ReadUInt32BigEndian(image.Slice(position, 4));
        chunk = new ContainerChunk(tag, position + 8, (int)size, position + 8 + (int)size + (int)(size & 1));
        return true;
    }

    private static bool ScrubWebpChunk(Span<byte> image, ContainerChunk chunk)
    {
        var data = image.Slice(chunk.Start, chunk.Length);
        if (chunk.Tag == WebpXmp) return XmpGpsBlanker.Blank(data);
        if (chunk.Tag != WebpExif) return false;
        // Some writers keep the JPEG "Exif\0\0" prefix inside the WebP chunk; the spec puts the TIFF header first.
        return ExifGpsEraser.Erase(data.StartsWith(ExifHeader) ? data[ExifHeader.Length..] : data);
    }

    private static uint FourCc(string code) => BinaryPrimitives.ReadUInt32BigEndian(Encoding.ASCII.GetBytes(code));
}

using System.Buffers.Binary;
using System.Text;
using InspectFlow.Modules.Media.Application;

namespace InspectFlow.Tests.Support;

/// <summary>TIFF (EXIF) block plus where its GPS IFD and data sit. <paramref name="GpsPointer"/>: IFD0 value holding the GPS IFD offset.</summary>
public sealed record ExifSample(byte[] Bytes, int GpsStart, int GpsEnd, int GpsPointer);

/// <summary>A photo plus the byte ranges a GPS scrub may change (everything else must stay identical).</summary>
public sealed record PhotoSample(byte[] Bytes, string MimeType, int TiffOffset, IReadOnlyList<Range> AllowedChanges);

/// <summary>
/// Synthesizes JPEG, PNG and WebP files whose EXIF and XMP carry a known GPS position next to an orientation and a
/// date taken, so tests can assert that exactly the location bytes changed. The pixel data is filler: nothing here
/// decodes images.
/// </summary>
public static class ExifPhotoBuilder
{
    public const ushort Orientation = 6;
    public const string DateTaken = "2026:10:17 09:30:00";
    public const string XmpLatitude = "53,20.7512N";
    public const string XmpLongitude = "6,15.3021W";
    public const string XmpCreateDate = "xmp:CreateDate=\"2026-10-17T09:30:00\"";

    public static readonly uint[] Latitude = [53, 1, 20, 1, 4512, 100]; // 53° 20' 45.12"
    public static readonly uint[] Longitude = [6, 1, 15, 1, 1803, 100];

    private const int EntrySize = 12;

    public static string Xmp(bool withGps = true) =>
        "<?xpacket begin=\"\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\">" +
        "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"\" " +
        "xmlns:exif=\"http://ns.adobe.com/exif/1.0/\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\" " + XmpCreateDate +
        (withGps ? $" exif:GPSLatitude=\"{XmpLatitude}\"><exif:GPSLongitude>{XmpLongitude}</exif:GPSLongitude>" : ">") +
        "</rdf:Description></rdf:RDF></x:xmpmeta><?xpacket end=\"w\"?>";

    /// <summary>IFD0 (orientation, Exif IFD, GPS IFD) → Exif IFD (date taken) → GPS IFD (version, lat/lon refs, lat/lon rationals).</summary>
    public static ExifSample Tiff(bool littleEndian, bool withGps = true)
    {
        var ifd0Entries = withGps ? 3 : 2;
        var exifIfd = 8 + 2 + ifd0Entries * EntrySize + 4;
        var date = exifIfd + 2 + EntrySize + 4;
        var gpsIfd = date + 20;
        var gpsEnd = gpsIfd + 2 + 5 * EntrySize + 4 + 48;
        var tiff = new TiffWriter(littleEndian, withGps ? gpsEnd : gpsIfd);
        tiff.Header();
        WriteIfd0(tiff, ifd0Entries, exifIfd, gpsIfd);
        WriteExifIfd(tiff, exifIfd, date);
        if (withGps) WriteGpsIfd(tiff, gpsIfd);
        return new ExifSample(tiff.Bytes, gpsIfd, gpsEnd, 8 + 2 + 2 * EntrySize + 8);
    }

    private static void WriteIfd0(TiffWriter tiff, int entries, int exifIfd, int gpsIfd)
    {
        tiff.U16(8, entries);
        tiff.Entry(10, 0x0112, 3, 1);
        tiff.U16(10 + 8, Orientation);
        tiff.Entry(22, 0x8769, 4, 1);
        tiff.U32(22 + 8, (uint)exifIfd);
        if (entries == 3) tiff.Entry(34, 0x8825, 4, 1);
        if (entries == 3) tiff.U32(34 + 8, (uint)gpsIfd);
    }

    private static void WriteExifIfd(TiffWriter tiff, int exifIfd, int date)
    {
        tiff.U16(exifIfd, 1);
        tiff.Entry(exifIfd + 2, 0x9003, 2, 20);
        tiff.U32(exifIfd + 2 + 8, (uint)date);
        tiff.Ascii(date, DateTaken + "\0");
    }

    private static void WriteGpsIfd(TiffWriter tiff, int gps)
    {
        var latitude = gps + 2 + 5 * EntrySize + 4;
        tiff.U16(gps, 5);
        tiff.Entry(gps + 2, 0x0000, 1, 4);
        tiff.Ascii(gps + 2 + 8, "\u0002\u0003\0\0");
        tiff.Entry(gps + 14, 0x0001, 2, 2);
        tiff.Ascii(gps + 14 + 8, "N\0");
        tiff.Entry(gps + 26, 0x0002, 5, 3);
        tiff.U32(gps + 26 + 8, (uint)latitude);
        tiff.Entry(gps + 38, 0x0003, 2, 2);
        tiff.Ascii(gps + 38 + 8, "W\0");
        tiff.Entry(gps + 50, 0x0004, 5, 3);
        tiff.U32(gps + 50 + 8, (uint)(latitude + 24));
        tiff.Rationals(latitude, Latitude);
        tiff.Rationals(latitude + 24, Longitude);
    }

    /// <summary>SOI, APP0 (JFIF), APP1 (Exif), APP1 (XMP), SOS with filler entropy data, EOI.</summary>
    public static PhotoSample Jpeg(ExifSample exif, string? xmp = null)
    {
        byte[] jfif = [0xFF, 0xE0, 0x00, 0x10, .. "JFIF\0"u8, 1, 1, 0, 0, 1, 0, 1, 0, 0];
        var exifSegment = JpegApp1([.. "Exif\0\0"u8, .. exif.Bytes]);
        var xmpSegment = xmp is null ? [] : JpegApp1([.. "http://ns.adobe.com/xap/1.0/\0"u8, .. Encoding.UTF8.GetBytes(xmp)]);
        byte[] scan = [0xFF, 0xDA, 0x00, 0x08, 1, 1, 0, 0, 0x3F, 0, 0x12, 0xFF, 0x00, 0x34, 0xFF, 0xD9];
        byte[] bytes = [0xFF, 0xD8, .. jfif, .. exifSegment, .. xmpSegment, .. scan];
        var tiffOffset = 2 + jfif.Length + 4 + 6;
        return new PhotoSample(bytes, "image/jpeg", tiffOffset, GpsRanges(bytes, exif, tiffOffset));
    }

    private static byte[] JpegApp1(byte[] payload)
    {
        var length = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)(payload.Length + 2));
        return [0xFF, 0xE1, .. length, .. payload];
    }

    /// <summary>Signature, IHDR, eXIf, iTXt (uncompressed XMP), IDAT, IEND — every chunk with a valid CRC.</summary>
    public static PhotoSample Png(ExifSample exif, string? xmp = null)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var header = PngChunk("IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 2, 0, 0, 0]);
        var exifChunk = PngChunk("eXIf", exif.Bytes);
        var xmpChunk = xmp is null ? [] : PngChunk("iTXt", [.. "XML:com.adobe.xmp\0"u8, 0, 0, 0, 0, .. Encoding.UTF8.GetBytes(xmp)]);
        byte[] bytes = [.. signature, .. header, .. exifChunk, .. xmpChunk, .. PngChunk("IDAT", [1, 2, 3]), .. PngChunk("IEND", [])];
        var tiffOffset = signature.Length + header.Length + 8;
        var crcs = new List<Range> { Crc(tiffOffset + exif.Bytes.Length) };
        if (xmp is not null) crcs.Add(Crc(tiffOffset + exif.Bytes.Length + 4 + xmpChunk.Length - 4));
        return new PhotoSample(bytes, "image/png", tiffOffset, [.. GpsRanges(bytes, exif, tiffOffset), .. crcs]);
    }

    private static Range Crc(int start) => new(start, start + 4);

    private static byte[] PngChunk(string type, byte[] data)
    {
        byte[] typed = [.. Encoding.ASCII.GetBytes(type), .. data];
        var length = new byte[4];
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        BinaryPrimitives.WriteUInt32BigEndian(crc, PngCrc32.Compute(typed));
        return [.. length, .. typed, .. crc];
    }

    /// <summary>
    /// RIFF/WEBP, VP8X (EXIF + XMP flags), an odd-sized VP8 chunk (exercises padding), EXIF, XMP.
    /// <paramref name="jpegStylePrefix"/> starts the EXIF chunk with "Exif\0\0", as some writers do.
    /// </summary>
    public static PhotoSample Webp(ExifSample exif, string? xmp = null, bool jpegStylePrefix = false)
    {
        var extended = WebpChunk("VP8X", [0x0C, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        var image = WebpChunk("VP8 ", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);
        var prefix = jpegStylePrefix ? "Exif\0\0"u8.ToArray() : [];
        var exifChunk = WebpChunk("EXIF", [.. prefix, .. exif.Bytes]);
        var xmpChunk = xmp is null ? [] : WebpChunk("XMP ", Encoding.UTF8.GetBytes(xmp));
        byte[] body = [.. "WEBP"u8, .. extended, .. image, .. exifChunk, .. xmpChunk];
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)body.Length);
        byte[] bytes = [.. "RIFF"u8, .. size, .. body];
        var tiffOffset = 12 + extended.Length + image.Length + 8 + prefix.Length;
        return new PhotoSample(bytes, "image/webp", tiffOffset, GpsRanges(bytes, exif, tiffOffset));
    }

    private static byte[] WebpChunk(string fourCc, byte[] data)
    {
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)data.Length);
        byte[] padding = data.Length % 2 == 1 ? [0] : [];
        return [.. Encoding.ASCII.GetBytes(fourCc), .. size, .. data, .. padding];
    }

    /// <summary>The EXIF GPS block plus every occurrence of the XMP coordinates.</summary>
    private static List<Range> GpsRanges(byte[] bytes, ExifSample exif, int tiffOffset)
    {
        var ranges = new List<Range> { new(tiffOffset + exif.GpsStart, tiffOffset + exif.GpsEnd) };
        var text = Encoding.Latin1.GetString(bytes);
        foreach (var value in new[] { XmpLatitude, XmpLongitude })
        {
            var at = text.IndexOf(value, StringComparison.Ordinal);
            if (at >= 0) ranges.Add(new Range(at, at + value.Length));
        }
        return ranges;
    }

    /// <summary>Writes TIFF fields in the block's byte order.</summary>
    private sealed class TiffWriter(bool littleEndian, int size)
    {
        public byte[] Bytes { get; } = new byte[size];

        public void Header()
        {
            Bytes[0] = Bytes[1] = (byte)(littleEndian ? 'I' : 'M');
            U16(2, 42);
            U32(4, 8);
        }

        public void Entry(int at, ushort tag, ushort type, uint count)
        {
            U16(at, tag);
            U16(at + 2, type);
            U32(at + 4, count);
        }

        public void U16(int at, int value)
        {
            if (littleEndian) BinaryPrimitives.WriteUInt16LittleEndian(Bytes.AsSpan(at), (ushort)value);
            else BinaryPrimitives.WriteUInt16BigEndian(Bytes.AsSpan(at), (ushort)value);
        }

        public void U32(int at, uint value) => WriteU32(Bytes.AsSpan(at), value, littleEndian);

        public void Ascii(int at, string text) => Encoding.Latin1.GetBytes(text).CopyTo(Bytes, at);

        public void Rationals(int at, uint[] values)
        {
            for (var i = 0; i < values.Length; i++) U32(at + i * 4, values[i]);
        }
    }

    /// <summary>The bytes the TIFF writer would emit for <paramref name="values"/> (to search for leftovers).</summary>
    public static byte[] RationalBytes(uint[] values, bool littleEndian)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) WriteU32(bytes.AsSpan(i * 4), values[i], littleEndian);
        return bytes;
    }

    private static void WriteU32(Span<byte> target, uint value, bool littleEndian)
    {
        if (littleEndian) BinaryPrimitives.WriteUInt32LittleEndian(target, value);
        else BinaryPrimitives.WriteUInt32BigEndian(target, value);
    }
}

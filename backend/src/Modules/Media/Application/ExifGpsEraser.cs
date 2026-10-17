using System.Buffers.Binary;

namespace InspectFlow.Modules.Media.Application;

/// <summary>
/// Wipes the GPS sub-IFD of an EXIF (TIFF) block in place: the GPS entries and their out-of-line values become
/// zeros and the IFD's entry count becomes 0. IFD0 keeps pointing at the now-empty IFD (a 0 pointer confuses some
/// readers) and every other tag — orientation, date taken, camera — is left byte for byte. The block never changes
/// length, so the container around it (JPEG segment, PNG chunk, WebP chunk) stays valid.
/// Example: <c>ExifGpsEraser.Erase(app1Payload[6..]) // true when coordinates were removed</c>
/// </summary>
internal static class ExifGpsEraser
{
    private const ushort GpsInfoTag = 0x8825;
    private const int EntrySize = 12;

    /// <returns>True when a GPS IFD with at least one entry was wiped; false (untouched) for anything unparseable.</returns>
    public static bool Erase(Span<byte> tiff)
    {
        if (!TiffBlock.TryOpen(tiff, out var block)) return false;
        var gpsIfd = FindGpsIfdOffset(block);
        return gpsIfd >= TiffBlock.HeaderSize && WipeIfd(block, gpsIfd);
    }

    private static long FindGpsIfdOffset(TiffBlock block)
    {
        var ifd0 = block.ReadU32(4);
        var count = block.ReadU16(ifd0);
        if (count <= 0 || !block.Contains(ifd0 + 2, count * EntrySize)) return -1;
        for (var i = 0; i < count; i++)
        {
            var entry = ifd0 + 2 + i * EntrySize;
            if (block.ReadU16(entry) == GpsInfoTag) return block.ReadU32(entry + 8);
        }
        return -1;
    }

    private static bool WipeIfd(TiffBlock block, long ifd)
    {
        var count = block.ReadU16(ifd);
        if (count <= 0 || !block.Contains(ifd + 2, count * EntrySize)) return false;
        for (var i = 0; i < count; i++) WipeEntry(block, ifd + 2 + i * EntrySize);
        block.WriteU16(ifd, 0); // the next-IFD offset is now read from the zeroed first entry: 0, no next IFD
        return true;
    }

    private static void WipeEntry(TiffBlock block, long entry)
    {
        var size = ValueSize(block.ReadU16(entry + 2), block.ReadU32(entry + 4));
        var valueOffset = block.ReadU32(entry + 8);
        // Values of 4 bytes or less live inside the entry itself, which is cleared below.
        if (size > 4 && valueOffset >= TiffBlock.HeaderSize && block.Contains(valueOffset, size)) block.Clear(valueOffset, size);
        block.Clear(entry, EntrySize);
    }

    /// <summary>Bytes taken by <paramref name="count"/> values of TIFF field <paramref name="type"/>; 0 for unknown types.</summary>
    private static long ValueSize(long type, long count) => count < 0 ? 0 : count * BytesPerValue(type);

    private static long BytesPerValue(long type) => type switch
    {
        1 or 2 or 6 or 7 => 1, // BYTE, ASCII, SBYTE, UNDEFINED
        3 or 8 => 2, // SHORT, SSHORT
        4 or 9 or 11 or 13 => 4, // LONG, SLONG, FLOAT, IFD
        5 or 10 or 12 => 8, // RATIONAL, SRATIONAL, DOUBLE
        _ => 0,
    };
}

/// <summary>
/// Bounds-checked view of a TIFF block in its own byte order ("II" little endian, "MM" big endian). Reads outside
/// the block return -1 instead of throwing, so a corrupt offset simply fails the lookup that used it.
/// </summary>
internal readonly ref struct TiffBlock
{
    public const int HeaderSize = 8;

    private readonly Span<byte> _bytes;
    private readonly bool _littleEndian;

    private TiffBlock(Span<byte> bytes, bool littleEndian)
    {
        _bytes = bytes;
        _littleEndian = littleEndian;
    }

    /// <summary>Accepts "II*\0" and "MM\0*" headers. Example: <c>TiffBlock.TryOpen(span, out var block)</c>.</summary>
    public static bool TryOpen(Span<byte> bytes, out TiffBlock block)
    {
        block = default;
        if (bytes.Length < HeaderSize) return false;
        var little = bytes[0] == 'I' && bytes[1] == 'I';
        if (!little && !(bytes[0] == 'M' && bytes[1] == 'M')) return false;
        block = new TiffBlock(bytes, little);
        return block.ReadU16(2) == 42;
    }

    public bool Contains(long offset, long length) => offset >= 0 && length >= 0 && offset + length <= _bytes.Length;

    public long ReadU16(long offset)
    {
        if (!Contains(offset, 2)) return -1;
        var span = _bytes.Slice((int)offset, 2);
        return _littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(span) : BinaryPrimitives.ReadUInt16BigEndian(span);
    }

    public long ReadU32(long offset)
    {
        if (!Contains(offset, 4)) return -1;
        var span = _bytes.Slice((int)offset, 4);
        return _littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(span) : BinaryPrimitives.ReadUInt32BigEndian(span);
    }

    public void WriteU16(long offset, ushort value)
    {
        if (!Contains(offset, 2)) return;
        var span = _bytes.Slice((int)offset, 2);
        if (_littleEndian) BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        else BinaryPrimitives.WriteUInt16BigEndian(span, value);
    }

    public void Clear(long offset, long length)
    {
        if (Contains(offset, length)) _bytes.Slice((int)offset, (int)length).Clear();
    }
}

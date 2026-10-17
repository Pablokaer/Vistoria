namespace InspectFlow.Modules.Media.Application;

/// <summary>
/// CRC-32 (ISO-HDLC, reflected polynomial 0xEDB88320) as PNG computes it over a chunk's type and data. Written here
/// rather than taking a package for one table and one loop.
/// Example: <c>PngCrc32.Compute("123456789"u8) // 0xCBF43926</c>
/// </summary>
internal static class PngCrc32
{
    private static readonly uint[] Table = BuildTable();

    public static uint Compute(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (var n = 0u; n < 256; n++) table[n] = TableEntry(n);
        return table;
    }

    private static uint TableEntry(uint n)
    {
        var c = n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        return c;
    }
}

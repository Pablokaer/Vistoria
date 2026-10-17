using System.Buffers.Binary;
using System.Text;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Domain;

public class PhotoLocationScrubberTests
{
    public static TheoryData<string> Formats => ["image/jpeg", "image/png", "image/webp"];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Jpeg_exif_gps_is_wiped_in_both_byte_orders_and_nothing_else_changes(bool littleEndian)
    {
        var exif = ExifPhotoBuilder.Tiff(littleEndian);
        var sample = ExifPhotoBuilder.Jpeg(exif);

        var (removed, scrubbed) = Scrub(sample);

        Assert.True(removed);
        AssertOnlyAllowedRangesChanged(sample, scrubbed);
        Assert.All(scrubbed[(sample.TiffOffset + exif.GpsStart)..(sample.TiffOffset + exif.GpsEnd)], b => Assert.Equal(0, b));
        AssertCoordinatesGone(scrubbed, littleEndian);
        // IFD0 still points at the (now empty) GPS IFD: a 0 pointer confuses some readers.
        Assert.Equal((uint)exif.GpsStart, ReadU32(scrubbed, sample.TiffOffset + exif.GpsPointer, littleEndian));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Date_taken_and_orientation_are_kept(bool littleEndian)
    {
        var sample = ExifPhotoBuilder.Jpeg(ExifPhotoBuilder.Tiff(littleEndian));

        var (_, scrubbed) = Scrub(sample);

        var orientation = scrubbed.AsSpan(sample.TiffOffset + 18, 2);
        Assert.Equal(ExifPhotoBuilder.Orientation, littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(orientation) : BinaryPrimitives.ReadUInt16BigEndian(orientation));
        Assert.Contains(ExifPhotoBuilder.DateTaken, Encoding.Latin1.GetString(scrubbed), StringComparison.Ordinal);
    }

    [Fact]
    public void Xmp_gps_values_become_spaces_and_the_segment_keeps_its_length()
    {
        var sample = ExifPhotoBuilder.Jpeg(ExifPhotoBuilder.Tiff(true, withGps: false), ExifPhotoBuilder.Xmp());

        var (removed, scrubbed) = Scrub(sample);

        Assert.True(removed);
        AssertOnlyAllowedRangesChanged(sample, scrubbed); // includes the APP1 length field: unchanged
        var text = Encoding.Latin1.GetString(scrubbed);
        Assert.Contains($"exif:GPSLatitude=\"{new string(' ', ExifPhotoBuilder.XmpLatitude.Length)}\"", text, StringComparison.Ordinal);
        Assert.Contains($"<exif:GPSLongitude>{new string(' ', ExifPhotoBuilder.XmpLongitude.Length)}</exif:GPSLongitude>", text, StringComparison.Ordinal);
        Assert.Contains(ExifPhotoBuilder.XmpCreateDate, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Png_exif_and_xmp_lose_gps_and_every_chunk_crc_stays_valid()
    {
        var sample = ExifPhotoBuilder.Png(ExifPhotoBuilder.Tiff(false), ExifPhotoBuilder.Xmp());

        var (removed, scrubbed) = Scrub(sample);

        Assert.True(removed);
        AssertOnlyAllowedRangesChanged(sample, scrubbed);
        AssertCoordinatesGone(scrubbed, littleEndian: false);
        AssertPngCrcsValid(scrubbed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Webp_exif_and_xmp_lose_gps(bool jpegStylePrefix)
    {
        var sample = ExifPhotoBuilder.Webp(ExifPhotoBuilder.Tiff(true), ExifPhotoBuilder.Xmp(), jpegStylePrefix);

        var (removed, scrubbed) = Scrub(sample);

        Assert.True(removed);
        AssertOnlyAllowedRangesChanged(sample, scrubbed);
        AssertCoordinatesGone(scrubbed, littleEndian: true);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Photos_without_a_location_are_left_byte_for_byte(string mimeType)
    {
        var sample = Build(mimeType, ExifPhotoBuilder.Tiff(true, withGps: false), ExifPhotoBuilder.Xmp(withGps: false));

        var (removed, scrubbed) = Scrub(sample);

        Assert.False(removed);
        Assert.Equal(sample.Bytes, scrubbed);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Truncated_photos_never_throw(string mimeType)
    {
        var sample = Build(mimeType, ExifPhotoBuilder.Tiff(false), ExifPhotoBuilder.Xmp());
        // A copy per length: scrubbing edits in place, and every prefix must start from the original bytes.
        for (var length = 0; length <= sample.Bytes.Length; length++)
            PhotoLocationScrubber.RemoveLocation(sample.Bytes.AsSpan(0, length).ToArray(), mimeType);
    }

    [Theory]
    [InlineData("pointer")]
    [InlineData("count")]
    [InlineData("magic")]
    public void Corrupt_exif_leaves_the_photo_untouched(string corruption)
    {
        var exif = ExifPhotoBuilder.Tiff(true);
        var sample = ExifPhotoBuilder.Jpeg(exif);
        var at = corruption switch
        {
            "pointer" => sample.TiffOffset + exif.GpsPointer, // GPS IFD offset far past the end
            "count" => sample.TiffOffset + exif.GpsStart, // 65535 GPS entries
            _ => sample.TiffOffset + 2, // TIFF magic other than 42
        };
        sample.Bytes[at] = sample.Bytes[at + 1] = 0xFF;
        var original = sample.Bytes.ToArray();

        Assert.False(PhotoLocationScrubber.RemoveLocation(sample.Bytes, "image/jpeg"));
        Assert.Equal(original, sample.Bytes);
    }

    [Fact]
    public void Random_bytes_behind_a_valid_signature_never_throw_or_change()
    {
        for (byte seed = 1; seed <= 50; seed++)
        {
            var filler = TestData.Jpeg(4096, seed); // the JPEG stand-in other upload tests use
            Assert.False(PhotoLocationScrubber.RemoveLocation(filler.AsSpan(), "image/jpeg"));
            Assert.Equal(TestData.Jpeg(4096, seed), filler);
            PhotoLocationScrubber.RemoveLocation(Noise(seed, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), "image/png");
            PhotoLocationScrubber.RemoveLocation(Noise(seed, [.. "RIFF\0\0\0\0WEBP"u8]), "image/webp");
        }
    }

    [Fact]
    public void Unsupported_types_are_ignored()
    {
        var sample = ExifPhotoBuilder.Jpeg(ExifPhotoBuilder.Tiff(true));
        Assert.False(PhotoLocationScrubber.RemoveLocation(sample.Bytes, "image/gif"));
    }

    [Fact]
    public void Xmp_blanker_handles_quotes_attributes_and_self_closing_elements()
    {
        var xmp = "<d exif:GPSLatitude='1,2N' xmp:Rating=\"5\"><exif:GPSAltitude rdf:parseType=\"x\">120/1</exif:GPSAltitude><exif:GPSMapDatum/></d>";
        var packet = Encoding.UTF8.GetBytes(xmp);

        Assert.True(XmpGpsBlanker.Blank(packet));

        Assert.Equal("<d exif:GPSLatitude='    ' xmp:Rating=\"5\"><exif:GPSAltitude rdf:parseType=\"x\">     </exif:GPSAltitude><exif:GPSMapDatum/></d>",
            Encoding.UTF8.GetString(packet));
    }

    [Fact]
    public void Png_crc_matches_the_standard_check_value() => Assert.Equal(0xCBF43926u, PngCrc32.Compute("123456789"u8));

    private static PhotoSample Build(string mimeType, ExifSample exif, string xmp) => mimeType switch
    {
        "image/jpeg" => ExifPhotoBuilder.Jpeg(exif, xmp),
        "image/png" => ExifPhotoBuilder.Png(exif, xmp),
        _ => ExifPhotoBuilder.Webp(exif, xmp),
    };

    private static (bool Removed, byte[] Bytes) Scrub(PhotoSample sample)
    {
        var copy = sample.Bytes.ToArray();
        return (PhotoLocationScrubber.RemoveLocation(copy, sample.MimeType), copy);
    }

    private static void AssertOnlyAllowedRangesChanged(PhotoSample sample, byte[] scrubbed)
    {
        Assert.Equal(sample.Bytes.Length, scrubbed.Length);
        var changed = Enumerable.Range(0, scrubbed.Length).Where(i => scrubbed[i] != sample.Bytes[i]).ToList();
        Assert.NotEmpty(changed);
        Assert.All(changed, i => Assert.Contains(sample.AllowedChanges, r => i >= r.Start.Value && i < r.End.Value));
    }

    private static void AssertCoordinatesGone(byte[] scrubbed, bool littleEndian)
    {
        Assert.Equal(-1, scrubbed.AsSpan().IndexOf(ExifPhotoBuilder.RationalBytes(ExifPhotoBuilder.Latitude, littleEndian)));
        Assert.Equal(-1, scrubbed.AsSpan().IndexOf(ExifPhotoBuilder.RationalBytes(ExifPhotoBuilder.Longitude, littleEndian)));
        var text = Encoding.Latin1.GetString(scrubbed);
        Assert.DoesNotContain(ExifPhotoBuilder.XmpLatitude, text, StringComparison.Ordinal);
        Assert.DoesNotContain(ExifPhotoBuilder.XmpLongitude, text, StringComparison.Ordinal);
    }

    private static void AssertPngCrcsValid(byte[] png)
    {
        for (var position = 8; position + 12 <= png.Length;)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position, 4));
            var expected = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position + 8 + length, 4));
            Assert.Equal(expected, PngCrc32.Compute(png.AsSpan(position + 4, length + 4)));
            position += 12 + length;
        }
    }

    private static uint ReadU32(byte[] bytes, int at, bool littleEndian) =>
        littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at)) : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(at));

    private static byte[] Noise(byte seed, byte[] signature)
    {
        var bytes = new byte[2048];
        new Random(seed).NextBytes(bytes);
        signature.CopyTo(bytes, 0);
        return bytes;
    }
}

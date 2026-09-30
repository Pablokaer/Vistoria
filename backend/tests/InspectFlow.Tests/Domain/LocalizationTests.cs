using InspectFlow.Shared.Localization;

namespace InspectFlow.Tests.Domain;

public class LocalizationTests
{
    [Theory]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("PT", "pt-BR")]
    [InlineData("en-GB", "en")]
    [InlineData("fr-FR", "en")]
    [InlineData(null, "en")]
    public void Resolve_maps_any_portuguese_tag_to_pt_BR_and_everything_else_to_English(string? culture, string expected) =>
        Assert.Equal(expected, SupportedLanguages.Resolve(culture));

    [Theory]
    [InlineData("en", true)]
    [InlineData("PT-br", true)]
    [InlineData("pt-PT", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupported_accepts_only_exact_supported_tags(string? language, bool expected) =>
        Assert.Equal(expected, SupportedLanguages.IsSupported(language));

    [Fact]
    public void Canonical_normalises_casing_and_rejects_unknown_tags_with_the_offending_value()
    {
        Assert.Equal("pt-BR", SupportedLanguages.Canonical("PT-br"));
        var error = Assert.Throws<ArgumentException>(() => SupportedLanguages.Canonical("de"));
        Assert.Contains("'de'", error.Message);
    }

    [Fact]
    public void LocalizedText_picks_the_language_and_logs_in_English()
    {
        var text = new LocalizedText("Name is required.", "O nome é obrigatório.");
        Assert.Equal("O nome é obrigatório.", text.In("pt-BR"));
        Assert.Equal("Name is required.", text.In("es"));
        Assert.Equal("Name is required.", text.ToString());
    }
}

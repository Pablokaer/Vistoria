using System.Globalization;

namespace InspectFlow.Shared.Localization;

/// <summary>
/// The languages the platform speaks. UI text follows the caller (Accept-Language); report and AI text follow
/// the company's report language, so a finalized report does not change with whoever opens it.
/// </summary>
public static class SupportedLanguages
{
    public const string English = "en";
    public const string PortugueseBrazil = "pt-BR";

    public static readonly IReadOnlyList<string> All = [English, PortugueseBrazil];

    /// <summary>
    /// Maps any culture tag to a supported language; anything that is not Portuguese falls back to English.
    /// Example: <c>SupportedLanguages.Resolve("pt-PT")</c> returns <c>"pt-BR"</c>.
    /// </summary>
    public static string Resolve(string? cultureName) =>
        cultureName is not null && cultureName.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? PortugueseBrazil : English;

    /// <summary>True only for an exact supported tag (case-insensitive) — used to validate user input.</summary>
    public static bool IsSupported(string? language) =>
        language is not null && All.Any(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase));

    /// <summary>The canonical spelling of a supported tag, e.g. "PT-br" → "pt-BR". Throws for unsupported input.</summary>
    public static string Canonical(string language) =>
        All.FirstOrDefault(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unsupported language '{language}'; expected one of: {string.Join(", ", All)}.", nameof(language));
}

/// <summary>
/// A user-facing text in every supported language. Thrown errors and report labels carry one of these;
/// the language is chosen only when the text reaches the reader (HTTP response, PDF).
/// Example: <c>new LocalizedText("Name is required.", "O nome é obrigatório.").In("pt-BR")</c>.
/// </summary>
public sealed record LocalizedText(string En, string PtBr)
{
    /// <summary>The text for a culture tag; unsupported tags get English.</summary>
    public string In(string? language) =>
        SupportedLanguages.Resolve(language) == SupportedLanguages.PortugueseBrazil ? PtBr : En;

    /// <summary>The text for the current thread's UI culture (set per request by the localization middleware).</summary>
    public string ForCurrentCulture() => In(CultureInfo.CurrentUICulture.Name);

    /// <summary>English, so logs and exception messages stay in one language.</summary>
    public override string ToString() => En;
}

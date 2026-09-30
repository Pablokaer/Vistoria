using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// The language the AI writes free text in, derived from the company's report language
/// (<see cref="SupportedLanguages"/>). <see cref="PromptName"/> is how the language is named to the model —
/// English names are followed most reliably.
/// </summary>
public sealed record AiOutputLanguage(string Code, string PromptName)
{
    public static readonly AiOutputLanguage English = new(SupportedLanguages.English, "British English");
    public static readonly AiOutputLanguage PortugueseBrazil = new(SupportedLanguages.PortugueseBrazil, "Brazilian Portuguese");

    /// <summary>
    /// Maps a company report language to the AI output language; unknown values fall back to English.
    /// Example: <c>AiOutputLanguage.For("pt-BR").PromptName // "Brazilian Portuguese"</c>
    /// </summary>
    public static AiOutputLanguage For(string? companyReportLanguage) =>
        SupportedLanguages.Resolve(companyReportLanguage) == SupportedLanguages.PortugueseBrazil ? PortugueseBrazil : English;
}

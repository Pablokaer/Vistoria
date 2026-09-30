using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// Failure texts of AI analyses. The background worker has no request language, so ai_analyses.error stores the
/// English text; <see cref="Localize"/> translates it when an analysis is returned to a user.
/// Example: <c>AiErrorTexts.Localize(analysis.Error)</c>.
/// </summary>
public static class AiErrorTexts
{
    public static readonly LocalizedText NotConfigured = new("The AI provider is not configured.", "O provedor de IA não está configurado.");
    public static readonly LocalizedText TimedOut = new("The AI service timed out. Please retry.", "O serviço de IA demorou demais para responder. Tente novamente.");
    public static readonly LocalizedText Unreachable = new("The AI service could not be reached. Please retry.", "Não foi possível acessar o serviço de IA. Tente novamente.");
    public static readonly LocalizedText CredentialsRejected = new("The AI provider rejected the configured credentials.", "O provedor de IA recusou as credenciais configuradas.");
    public static readonly LocalizedText RateLimited = new("The AI provider is busy (rate limit). Please retry shortly.", "O provedor de IA está ocupado (limite de uso). Tente novamente em instantes.");
    public static readonly LocalizedText CouldNotProcess = new("The AI provider could not process these photos.", "O provedor de IA não conseguiu processar estas fotos.");
    public static readonly LocalizedText ProviderError = new("The AI service returned an error. Please retry.", "O serviço de IA retornou um erro. Tente novamente.");
    public static readonly LocalizedText Declined = new("The AI declined to analyse these photos. Please describe the room manually.", "A IA se recusou a analisar estas fotos. Descreva o cômodo manualmente.");
    public static readonly LocalizedText EmptyResponse = new("The AI returned an empty response.", "A IA retornou uma resposta vazia.");
    public static readonly LocalizedText UnexpectedFormat = new("The AI returned an unexpected format.", "A IA retornou um formato inesperado.");
    public static readonly LocalizedText PhotosGone = new("The photos for this analysis are no longer available.", "As fotos desta análise não estão mais disponíveis.");
    public static readonly LocalizedText NotConfiguredOnServer = new("AI descriptions are not configured on this server. Please write the description manually.",
        "As descrições por IA não estão configuradas neste servidor. Escreva a descrição manualmente.");
    public static readonly LocalizedText Generic = new("The AI service could not process these photos. You can retry or write the description manually.",
        "O serviço de IA não conseguiu processar estas fotos. Tente novamente ou escreva a descrição manualmente.");

    private static readonly Dictionary<string, LocalizedText> ByEnglish = new[]
    {
        NotConfigured, TimedOut, Unreachable, CredentialsRejected, RateLimited, CouldNotProcess,
        ProviderError, Declined, EmptyResponse, UnexpectedFormat, PhotosGone, NotConfiguredOnServer, Generic,
    }.ToDictionary(t => t.En, StringComparer.Ordinal);

    /// <summary>The stored English error in the current UI culture; unknown texts (older rows) are returned as stored.</summary>
    public static string? Localize(string? storedEnglish) =>
        storedEnglish is not null && ByEnglish.TryGetValue(storedEnglish, out var text) ? text.ForCurrentLanguage() : storedEnglish;
}

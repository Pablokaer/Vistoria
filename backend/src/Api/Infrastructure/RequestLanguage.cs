using InspectFlow.Shared.Localization;
using Microsoft.Net.Http.Headers;

namespace InspectFlow.Api.Infrastructure;

/// <summary>
/// Picks the response language from Accept-Language. A custom middleware instead of UseRequestLocalization because
/// the app runs with invariant globalization (no pt-BR CultureInfo), that middleware would also change number/date
/// formatting, and it cannot map pt-PT to pt-BR; here only <see cref="CurrentLanguage"/> changes.
/// </summary>
public static class RequestLanguage
{
    /// <summary>
    /// The supported language of a request, read straight from the header so it also works where the middleware's
    /// culture does not reach (exception handler, rate-limiter rejection). Example: "pt-PT;q=0.9, en;q=0.5" → "pt-BR".
    /// </summary>
    public static string Of(HttpContext context) => FromAcceptLanguage(context.Request.Headers.AcceptLanguage.ToString());

    public static string FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header) || !StringWithQualityHeaderValue.TryParseList([header], out var values))
            return SupportedLanguages.English;
        var preferred = values.OrderByDescending(v => v.Quality ?? 1.0)
            .Select(v => v.Value.Value ?? string.Empty)
            .FirstOrDefault(IsKnownLanguage);
        return SupportedLanguages.Resolve(preferred);
    }

    /// <summary>Sets <see cref="CurrentLanguage"/> for the rest of the request so application code can use LocalizedText.ForCurrentLanguage().</summary>
    public static IApplicationBuilder UseRequestLanguage(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        CurrentLanguage.Set(Of(context));
        await next();
    });

    private static bool IsKnownLanguage(string tag) =>
        tag.StartsWith("pt", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("en", StringComparison.OrdinalIgnoreCase);
}

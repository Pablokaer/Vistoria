using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Api.Infrastructure;

/// <summary>
/// Maps application exceptions to RFC 7807 problem details with a stable "code". Texts are written in the
/// request's language (Accept-Language); logs keep the English message.
/// </summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    private static readonly LocalizedText ConcurrentChange = new("The data was changed by someone else. Refresh and try again.",
        "Os dados foram alterados por outra pessoa. Atualize a página e tente novamente.");
    private static readonly LocalizedText Immutable = new("Finalized records cannot be changed.", "Registros finalizados não podem ser alterados.");
    private static readonly LocalizedText BadRequest = new("The request is invalid.", "A solicitação é inválida.");
    private static readonly LocalizedText ServerError = new("An unexpected error occurred.", "Ocorreu um erro inesperado.");

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, text) = Classify(exception);
        if (status >= 500) logger.LogError(exception, "Unhandled exception");
        else logger.LogInformation("Request failed with {Status} {Code}: {Message}", status, code, exception.Message);

        var language = RequestLanguage.Of(httpContext);
        httpContext.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = text.In(language), Type = $"https://inspectflow.dev/errors/{code}" };
        details.Extensions["code"] = code;
        AddLocalizedExtras(details, exception, language);
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = details, Exception = exception });
    }

    private static (int Status, string Code, LocalizedText Text) Classify(Exception exception) => exception switch
    {
        ValidationException v => (StatusCodes.Status400BadRequest, v.Code, v.Text),
        UnauthorizedException u => (StatusCodes.Status401Unauthorized, u.Code, u.Text),
        ForbiddenException f => (StatusCodes.Status403Forbidden, f.Code, f.Text),
        NotFoundException n => (StatusCodes.Status404NotFound, n.Code, n.Text),
        ConflictException c => (StatusCodes.Status409Conflict, c.Code, c.Text),
        TooManyAttemptsException t => (StatusCodes.Status429TooManyRequests, t.Code, t.Text),
        DomainRuleException d => (StatusCodes.Status422UnprocessableEntity, d.Code, d.Text),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "conflict", ConcurrentChange),
        ImmutableRecordException => (StatusCodes.Status409Conflict, "immutable", Immutable),
        BadHttpRequestException b => (b.StatusCode, "bad_request", BadRequest),
        _ => (StatusCodes.Status500InternalServerError, "server_error", ServerError),
    };

    /// <summary>Field errors ("errors") and rule details ("details"), both in the request's language.</summary>
    private static void AddLocalizedExtras(ProblemDetails details, Exception exception, string language)
    {
        if (exception is ValidationException { Errors.Count: > 0 } v)
            details.Extensions["errors"] = v.Errors.ToDictionary(e => e.Key, e => e.Value.Select(t => t.In(language)).ToArray());
        if (exception is DomainRuleException { Details.Count: > 0 } d)
            details.Extensions["details"] = d.Details.Select(t => t.In(language)).ToList();
    }
}

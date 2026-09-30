using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Shared.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Api.Infrastructure;

/// <summary>Maps application exceptions to RFC 7807 problem details with a stable "code".</summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var http = httpContext;
        var (status, code, title, extra) = exception switch
        {
            ValidationException v => (StatusCodes.Status400BadRequest, v.Code, v.Message, (object?)v.Errors),
            UnauthorizedException u => (StatusCodes.Status401Unauthorized, u.Code, u.Message, null),
            ForbiddenException f => (StatusCodes.Status403Forbidden, f.Code, f.Message, null),
            NotFoundException n => (StatusCodes.Status404NotFound, n.Code, n.Message, null),
            ConflictException c => (StatusCodes.Status409Conflict, c.Code, c.Message, null),
            TooManyAttemptsException t => (StatusCodes.Status429TooManyRequests, t.Code, t.Message, null),
            DomainRuleException d => (StatusCodes.Status422UnprocessableEntity, d.Code, d.Message, d.Details),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "conflict", "The data was changed by someone else. Refresh and try again.", null),
            ImmutableRecordException => (StatusCodes.Status409Conflict, "immutable", "Finalized records cannot be changed.", null),
            BadHttpRequestException b => (b.StatusCode, "bad_request", "The request is invalid.", null),
            _ => (StatusCodes.Status500InternalServerError, "server_error", "An unexpected error occurred.", null),
        };

        if (status >= 500) logger.LogError(exception, "Unhandled exception");
        else logger.LogInformation("Request failed with {Status} {Code}: {Message}", status, code, exception.Message);

        http.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title, Type = $"https://inspectflow.dev/errors/{code}" };
        details.Extensions["code"] = code;
        if (extra is IReadOnlyDictionary<string, string[]> errors && errors.Count > 0) details.Extensions["errors"] = errors;
        if (extra is IReadOnlyList<string> list && list.Count > 0) details.Extensions["details"] = list;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = details, Exception = exception });
    }
}

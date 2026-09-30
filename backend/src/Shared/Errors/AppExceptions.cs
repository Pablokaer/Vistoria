namespace InspectFlow.Shared.Errors;

/// <summary>Base type for expected, user-facing failures. Mapped to ProblemDetails by the API.</summary>
public abstract class AppException : Exception
{
    protected AppException(string code, string message) : base(message) => Code = code;

    /// <summary>Stable machine-readable error code (e.g. "inspection.invalid_transition").</summary>
    public string Code { get; }
}

/// <summary>Resource does not exist or the caller is not allowed to know it exists.</summary>
public sealed class NotFoundException(string entity, object? id = null)
    : AppException("not_found", id is null ? $"{entity} was not found." : $"{entity} '{id}' was not found.");

/// <summary>The caller is authenticated but not allowed to perform the action.</summary>
public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException("forbidden", message);

/// <summary>Input validation failure.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base("validation_failed", message) => Errors = errors ?? new Dictionary<string, string[]>();

    public ValidationException(string field, string message)
        : this(message, new Dictionary<string, string[]> { [field] = [message] }) { }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

/// <summary>A business rule was violated (invalid state transition, incomplete inspection, ...).</summary>
public sealed class DomainRuleException : AppException
{
    public DomainRuleException(string code, string message, IReadOnlyList<string>? details = null)
        : base(code, message) => Details = details ?? [];

    public IReadOnlyList<string> Details { get; }
}

/// <summary>The operation lost a race against a concurrent change (optimistic concurrency).</summary>
public sealed class ConflictException(string message, string code = "conflict") : AppException(code, message);

/// <summary>Too many attempts (e.g. private invitation access code).</summary>
public sealed class TooManyAttemptsException(string message) : AppException("too_many_attempts", message);

/// <summary>Authentication failed or is missing (HTTP 401).</summary>
public sealed class UnauthorizedException(string message = "Authentication failed.") : AppException("unauthorized", message);

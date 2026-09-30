using InspectFlow.Shared.Localization;

namespace InspectFlow.Shared.Errors;

/// <summary>
/// Base type for expected, user-facing failures. Mapped to ProblemDetails by the API, which picks the
/// language of <see cref="Text"/> from the request; <see cref="Exception.Message"/> stays English for logs.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string code, LocalizedText text) : base(text.En)
    {
        Code = code;
        Text = text;
    }

    /// <summary>Stable machine-readable error code (e.g. "inspection.invalid_transition").</summary>
    public string Code { get; }

    /// <summary>The user-facing message in every supported language.</summary>
    public LocalizedText Text { get; }
}

/// <summary>Resource does not exist or the caller is not allowed to know it exists.</summary>
/// <param name="entity">The entity name in every language, e.g. <c>EntityNames.Room</c>.</param>
public sealed class NotFoundException(LocalizedText entity, object? id = null) : AppException("not_found", Describe(entity, id))
{
    private static LocalizedText Describe(LocalizedText entity, object? id) => id is null
        ? new($"{entity.En} was not found.", $"{entity.PtBr} não encontrado(a).")
        : new($"{entity.En} '{id}' was not found.", $"{entity.PtBr} '{id}' não encontrado(a).");
}

/// <summary>The caller is authenticated but not allowed to perform the action.</summary>
public sealed class ForbiddenException(LocalizedText? text = null)
    : AppException("forbidden", text ?? new("You are not allowed to perform this action.", "Você não tem permissão para realizar esta ação."));

/// <summary>Input validation failure.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(LocalizedText text, IReadOnlyDictionary<string, LocalizedText[]>? errors = null)
        : base("validation_failed", text) => Errors = errors ?? new Dictionary<string, LocalizedText[]>();

    /// <summary>One invalid field. Example: <c>new ValidationException("Name", new("Name is required.", "O nome é obrigatório."))</c>.</summary>
    public ValidationException(string field, LocalizedText text)
        : this(text, new Dictionary<string, LocalizedText[]> { [field] = [text] }) { }

    public IReadOnlyDictionary<string, LocalizedText[]> Errors { get; }
}

/// <summary>A business rule was violated (invalid state transition, incomplete inspection, ...).</summary>
public sealed class DomainRuleException : AppException
{
    public DomainRuleException(string code, LocalizedText text, IReadOnlyList<LocalizedText>? details = null)
        : base(code, text) => Details = details ?? [];

    public IReadOnlyList<LocalizedText> Details { get; }
}

/// <summary>The operation lost a race against a concurrent change (optimistic concurrency).</summary>
public sealed class ConflictException(LocalizedText text, string code = "conflict") : AppException(code, text);

/// <summary>Too many attempts (e.g. private invitation access code).</summary>
public sealed class TooManyAttemptsException(LocalizedText text) : AppException("too_many_attempts", text);

/// <summary>Authentication failed or is missing (HTTP 401).</summary>
public sealed class UnauthorizedException(LocalizedText? text = null)
    : AppException("unauthorized", text ?? new("Authentication failed.", "Falha na autenticação."));

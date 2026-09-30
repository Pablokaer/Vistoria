using System.Text.RegularExpressions;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Identity;

namespace InspectFlow.Modules.Identity.Application;

/// <summary>
/// ASP.NET Core Identity describes errors in English only; this maps its stable error codes to both languages.
/// Unknown codes keep Identity's English description in both, rather than failing.
/// Example: <c>IdentityErrorTexts.Of(new IdentityError { Code = "PasswordRequiresDigit" }).PtBr</c>.
/// </summary>
public static partial class IdentityErrorTexts
{
    private static readonly Dictionary<string, string> PortugueseByCode = new()
    {
        ["DuplicateUserName"] = "Já existe uma conta com este e-mail.",
        ["DuplicateEmail"] = "Já existe uma conta com este e-mail.",
        ["InvalidEmail"] = "O e-mail informado é inválido.",
        ["InvalidUserName"] = "O e-mail informado é inválido.",
        ["PasswordRequiresDigit"] = "A senha deve conter pelo menos um número (0-9).",
        ["PasswordRequiresLower"] = "A senha deve conter pelo menos uma letra minúscula (a-z).",
        ["PasswordRequiresUpper"] = "A senha deve conter pelo menos uma letra maiúscula (A-Z).",
        ["PasswordRequiresNonAlphanumeric"] = "A senha deve conter pelo menos um caractere especial.",
        ["PasswordMismatch"] = "Senha incorreta.",
    };

    private static readonly LocalizedText DuplicateAccount =
        new("An account with this email already exists.", "Já existe uma conta com este e-mail.");

    public static LocalizedText Of(IdentityError error)
    {
        if (error.Code is "DuplicateUserName" or "DuplicateEmail") return DuplicateAccount;
        if (error.Code == "PasswordTooShort") return PasswordTooShort(error.Description);
        if (error.Code == "PasswordRequiresUniqueChars") return RequiresUniqueChars(error.Description);
        return new(error.Description, PortugueseByCode.GetValueOrDefault(error.Code, error.Description));
    }

    // Identity puts the configured minimum in its own English text; reuse it instead of reading options twice.
    private static LocalizedText PasswordTooShort(string description) =>
        new(description, $"A senha deve ter pelo menos {FirstNumber(description)} caracteres.");

    private static LocalizedText RequiresUniqueChars(string description) =>
        new(description, $"A senha deve usar pelo menos {FirstNumber(description)} caracteres diferentes.");

    private static string FirstNumber(string text) => NumberPattern().Match(text).Value;

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberPattern();
}

using InspectFlow.Api.Infrastructure;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace InspectFlow.Tests.Domain;

public class ErrorLocalizationTests
{
    /// <summary>Captures the problem details the handler writes instead of serializing them to a response.</summary>
    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }

    private static async Task<ProblemDetails> HandleAsync(Exception exception, string? acceptLanguage)
    {
        var problems = new RecordingProblemDetailsService();
        var handler = new AppExceptionHandler(NullLogger<AppExceptionHandler>.Instance, problems);
        var http = new DefaultHttpContext();
        if (acceptLanguage is not null) http.Request.Headers.AcceptLanguage = acceptLanguage;
        await handler.TryHandleAsync(http, exception, CancellationToken.None);
        return problems.Written!;
    }

    [Theory]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-PT,pt;q=0.9", "pt-BR")]
    [InlineData("pt", "pt-BR")]
    [InlineData("fr-FR, pt;q=0.5", "pt-BR")]
    [InlineData("en;q=0.4, pt-BR;q=0.8", "pt-BR")]
    [InlineData("en-GB", "en")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void Accept_language_picks_the_best_supported_language(string? header, string expected) =>
        Assert.Equal(expected, RequestLanguage.FromAcceptLanguage(header));

    [Theory]
    [InlineData("pt-BR", "O nome é obrigatório.")]
    [InlineData("pt-PT", "O nome é obrigatório.")]
    [InlineData(null, "Name is required.")]
    [InlineData("en", "Name is required.")]
    [InlineData("fr", "Name is required.")]
    public async Task Title_follows_the_request_language(string? header, string expected)
    {
        var problem = await HandleAsync(new ValidationException("Name", new("Name is required.", "O nome é obrigatório.")), header);
        Assert.Equal(expected, problem.Title);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Field_errors_and_rule_details_are_localized()
    {
        var validation = await HandleAsync(new ValidationException("Name", new("Name is required.", "O nome é obrigatório.")), "pt-BR");
        var errors = Assert.IsType<Dictionary<string, string[]>>(validation.Extensions["errors"]);
        Assert.Equal(["O nome é obrigatório."], errors["Name"]);

        var rule = new DomainRuleException("room.incomplete", new("Kitchen cannot be completed yet.", "Cozinha ainda não pode ser concluído."),
            [new("Kitchen: a room description is required.", "Cozinha: a descrição do cômodo é obrigatória.")]);
        var details = Assert.IsType<List<string>>((await HandleAsync(rule, "pt-BR")).Extensions["details"]);
        Assert.Equal(["Cozinha: a descrição do cômodo é obrigatória."], details);
    }

    [Fact]
    public async Task Fixed_fallback_texts_are_localized_and_exception_message_stays_English()
    {
        var problem = await HandleAsync(new InvalidOperationException("boom"), "pt-BR");
        Assert.Equal("Ocorreu um erro inesperado.", problem.Title);
        Assert.Equal("Name is required.", new ValidationException("Name", new("Name is required.", "O nome é obrigatório.")).Message);
    }

    [Fact]
    public void Not_found_names_the_entity_and_id_in_both_languages()
    {
        var ex = new NotFoundException(EntityNames.Room, 42);
        Assert.Equal("Room '42' was not found.", ex.Text.En);
        Assert.Equal("Cômodo '42' não encontrado(a).", ex.Text.PtBr);
    }

    [Theory]
    [InlineData("PasswordRequiresDigit", "Passwords must have at least one digit ('0'-'9').", "A senha deve conter pelo menos um número (0-9).")]
    [InlineData("PasswordTooShort", "Passwords must be at least 8 characters.", "A senha deve ter pelo menos 8 caracteres.")]
    [InlineData("DuplicateEmail", "Email 'a@b.c' is already taken.", "Já existe uma conta com este e-mail.")]
    [InlineData("SomethingNew", "Some new rule.", "Some new rule.")]
    public void Identity_errors_are_mapped_by_code(string code, string description, string expectedPortuguese) =>
        Assert.Equal(expectedPortuguese, IdentityErrorTexts.Of(new IdentityError { Code = code, Description = description }).PtBr);

    [Fact]
    public async Task Stored_AI_errors_are_translated_for_the_current_request_language()
    {
        await Task.Run(() =>
        {
            CurrentLanguage.Set("pt-BR");
            Assert.Equal(AiErrorTexts.TimedOut.PtBr, AiErrorTexts.Localize(AiErrorTexts.TimedOut.En));
            Assert.Equal("legacy text", AiErrorTexts.Localize("legacy text"));
            Assert.Null(AiErrorTexts.Localize(null));
        });
        // AsyncLocal: the language set inside the task does not leak into this test's context.
        Assert.Equal(SupportedLanguages.English, CurrentLanguage.Get());
    }
}

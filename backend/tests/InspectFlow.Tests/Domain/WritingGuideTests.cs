using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using InspectFlow.Infrastructure.AI;
using InspectFlow.Modules.AI.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InspectFlow.Tests.Domain;

/// <summary>
/// The writing guide is prompt content maintained by the team: these tests catch a broken edit (missing version or
/// marker) and prove the guide and the company's output language reach the model.
/// </summary>
public class WritingGuideTests
{
    private sealed class CapturingOpenAiHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var content = """{"summary":"Chip","description":"Lascado.","location":null,"confidence":0.8}""";
            var body = new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = content } }) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public void Embedded_guide_has_a_version_and_sends_only_the_model_section()
    {
        var guide = InspectionWritingGuide.Current;
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}\.\d+$", guide.Version);
        Assert.Contains("Condition vocabulary", guide.ModelText, StringComparison.Ordinal);
        Assert.DoesNotContain("How to grow this guide", guide.ModelText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("# no version line\n=== GUIDE FOR THE MODEL ===\ntext", "guide-version:")]
    [InlineData("guide-version: 2026-01-01.1\nno marker here", "=== GUIDE FOR THE MODEL ===")]
    [InlineData("guide-version: 2026-01-01.1\n=== GUIDE FOR THE MODEL ===\n   ", "no text after")]
    public void Broken_guides_are_rejected_with_a_message_naming_the_problem(string markdown, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => InspectionWritingGuide.Parse(markdown));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Company_report_language_maps_to_the_ai_output_language()
    {
        Assert.Equal(AiOutputLanguage.PortugueseBrazil, AiOutputLanguage.For("pt-BR"));
        Assert.Equal(AiOutputLanguage.PortugueseBrazil, AiOutputLanguage.For("pt"));
        Assert.Equal(AiOutputLanguage.English, AiOutputLanguage.For("en"));
        Assert.Equal(AiOutputLanguage.English, AiOutputLanguage.For(null));
    }

    [Fact]
    public void System_prompt_keeps_safety_first_then_language_then_guide_and_codes_in_english()
    {
        var prompt = AiPrompts.SystemPrompt(AiOutputLanguage.PortugueseBrazil);
        var safety = prompt.IndexOf("Describe ONLY what is clearly visible", StringComparison.Ordinal);
        var language = prompt.IndexOf("Brazilian Portuguese (pt-BR)", StringComparison.Ordinal);
        var guide = prompt.IndexOf(InspectionWritingGuide.Current.ModelText[..40], StringComparison.Ordinal);
        Assert.True(safety >= 0 && safety < language && language < guide, $"order: safety {safety}, language {language}, guide {guide}");
        Assert.Contains("condition codes", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("British English", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Prompt_version_records_code_guide_and_language()
    {
        Assert.Equal($"{AiPrompts.Version}+g{InspectionWritingGuide.Current.Version}+pt-BR", AiPrompts.FullVersion(AiOutputLanguage.PortugueseBrazil));
        Assert.True(AiPrompts.FullVersion(AiOutputLanguage.PortugueseBrazil).Length <= 50, "prompt_version column is 50 characters");
    }

    [Fact]
    public async Task OpenAi_request_carries_the_guide_and_the_output_language_in_the_system_message()
    {
        var handler = new CapturingOpenAiHandler();
        var options = Options.Create(new AiOptions { OpenAI = new OpenAiOptions { ApiKey = "sk-test-key", Model = "m" } });
        var service = new OpenAiImageAnalysisService(new HttpClient(handler), options, NullLogger<OpenAiImageAnalysisService>.Instance);

        await service.AnalyzeDefectAsync(new DefectAnalysisRequest("Cozinha", "lascado", [new AnalysisImage("image/png", [1])], AiOutputLanguage.PortugueseBrazil));

        var system = JsonNode.Parse(handler.RequestBody!)!["messages"]![0]!["content"]!.GetValue<string>();
        Assert.Contains("Brazilian Portuguese", system, StringComparison.Ordinal);
        Assert.Contains("Defect description", system, StringComparison.Ordinal);
    }

    [Fact]
    public void Mock_label_names_the_language_and_keeps_the_development_prefix()
    {
        var label = MockImageAnalysisService.LabelFor(AiOutputLanguage.PortugueseBrazil);
        Assert.StartsWith("[Development mock AI", label, StringComparison.Ordinal);
        Assert.EndsWith("Brazilian Portuguese]", label, StringComparison.Ordinal);
    }
}

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using InspectFlow.Infrastructure.AI;
using InspectFlow.Modules.AI.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InspectFlow.Tests.Domain;

/// <summary>Verifies the OpenAI adapter contract without network access (fake HTTP handler).</summary>
public class OpenAiImageAnalysisServiceTests
{
    private const string Key = "sk-test-secret-key-123";

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static (OpenAiImageAnalysisService Service, FakeHandler Handler) Create(HttpStatusCode status, string body)
    {
        var handler = new FakeHandler(status, body);
        var options = Options.Create(new AiOptions { OpenAI = new OpenAiOptions { ApiKey = Key, Model = "test-model" } });
        return (new OpenAiImageAnalysisService(new HttpClient(handler), options, NullLogger<OpenAiImageAnalysisService>.Instance), handler);
    }

    private static string Completion(object content) => new JsonObject
    {
        ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = System.Text.Json.JsonSerializer.Serialize(content) } }),
    }.ToJsonString();

    [Fact]
    public async Task Sends_images_with_strict_schema_and_safety_prompt_and_parses_structured_output()
    {
        var result = new
        {
            roomType = "bedroom",
            walls = new { color = "white", material = "paint", condition = "good", notes = (string?)null },
            ceiling = new { color = "white", material = (string?)null, condition = "good", notes = (string?)null },
            floor = new { color = "beige", material = "carpet", condition = "fair", notes = "light wear" },
            visibleItems = new[] { new { name = "bed", condition = "good", notes = (string?)null } },
            observedDefects = Array.Empty<object>(),
            cleanliness = "clean",
            limitations = new[] { "Ceiling partly out of frame" },
            description = "No visible damage is apparent in the provided images.",
        };
        var (service, handler) = Create(HttpStatusCode.OK, Completion(result));
        var analysis = await service.AnalyzeRoomAsync(new RoomAnalysisRequest("Bedroom 1", "Bedroom",
            [new AnalysisImage("image/jpeg", [1, 2, 3])], AiOutputLanguage.English));

        Assert.Equal("fair", analysis.Floor!.Condition);
        Assert.Equal("bed", analysis.VisibleItems[0].Name);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme);
        Assert.EndsWith("/chat/completions", handler.Request.RequestUri!.AbsolutePath, StringComparison.Ordinal);

        var body = JsonNode.Parse(handler.RequestBody!)!;
        Assert.Equal("json_schema", body["response_format"]!["type"]!.GetValue<string>());
        Assert.True(body["response_format"]!["json_schema"]!["strict"]!.GetValue<bool>());
        Assert.Contains("Do NOT determine responsibility", body["messages"]![0]!["content"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.StartsWith("data:image/jpeg;base64,", body["messages"]![1]!["content"]![1]!["image_url"]!["url"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.DoesNotContain(Key, handler.RequestBody!, StringComparison.Ordinal); // key only in the header
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Provider_errors_become_safe_messages_without_secrets(HttpStatusCode status)
    {
        var (service, _) = Create(status, "{\"error\":{\"message\":\"Incorrect API key provided: " + Key + "\"}}");
        var ex = await Assert.ThrowsAsync<AiProviderException>(() =>
            service.AnalyzeDefectAsync(new DefectAnalysisRequest("Kitchen", "crack", [new AnalysisImage("image/png", [1])], AiOutputLanguage.English)));
        Assert.DoesNotContain(Key, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refusals_are_reported_so_the_agent_writes_manually()
    {
        var body = """{"choices":[{"message":{"content":null,"refusal":"I can't help with that."}}]}""";
        var (service, _) = Create(HttpStatusCode.OK, body);
        var ex = await Assert.ThrowsAsync<AiProviderException>(() =>
            service.CompareRoomAsync(new RoomComparisonRequest("Kitchen", null, [], [], null, [], [new AnalysisImage("image/png", [1])], AiOutputLanguage.English)));
        Assert.Contains("manually", ex.Message, StringComparison.Ordinal);
    }
}

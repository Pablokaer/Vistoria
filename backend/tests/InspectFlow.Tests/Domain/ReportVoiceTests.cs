using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using InspectFlow.Infrastructure.AI;
using InspectFlow.Modules.AI.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InspectFlow.Tests.Domain;

/// <summary>
/// AI drafts must read like the inspector's own report entry (prompt v3, guide 2026-10-17.1): room overview first,
/// walls and floor always covered, defects described on the surface they are on, never "the image shows".
/// These tests pin the rules that reach the model and the mock's development drafts.
/// </summary>
public class ReportVoiceTests
{
    /// <summary>Records the request the OpenAI adapter sends and answers with a minimal valid room or defect result.</summary>
    private sealed class RecordingOpenAiHandler(string resultJson) : HttpMessageHandler
    {
        public JsonNode? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var body = new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = resultJson } }) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        }
    }

    private const string RoomResultJson = """
        {"roomType":"Bedroom","walls":{"color":null,"material":null,"condition":"good","notes":null},
         "ceiling":{"color":null,"material":null,"condition":"good","notes":null},
         "floor":{"color":null,"material":null,"condition":"good","notes":null},
         "visibleItems":[],"observedDefects":[],"cleanliness":null,"limitations":[],"description":"Bedroom."}
        """;

    private const string DefectResultJson = """{"summary":"Chip","description":"Chip.","location":null,"confidence":0.8}""";

    private static readonly AnalysisImage Photo = new("image/png", [1]);

    private static (OpenAiImageAnalysisService Service, RecordingOpenAiHandler Handler) OpenAi(string resultJson)
    {
        var handler = new RecordingOpenAiHandler(resultJson);
        var options = Options.Create(new AiOptions { OpenAI = new OpenAiOptions { ApiKey = "sk-test-key", Model = "m" } });
        return (new OpenAiImageAnalysisService(new HttpClient(handler), options, NullLogger<OpenAiImageAnalysisService>.Instance), handler);
    }

    private static string SystemText(JsonNode request) => request["messages"]![0]!["content"]!.GetValue<string>();

    private static string UserText(JsonNode request) => request["messages"]![1]!["content"]![0]!["text"]!.GetValue<string>();

    [Fact]
    public async Task Room_request_requires_a_room_overview_walls_floor_and_defect_close_ups()
    {
        var (service, handler) = OpenAi(RoomResultJson);

        await service.AnalyzeRoomAsync(new RoomAnalysisRequest("Bedroom 1", "Bedroom", [Photo], AiOutputLanguage.English));

        var user = UserText(handler.Request!);
        Assert.Contains("The walls and the floor must always be covered", user, StringComparison.Ordinal);
        Assert.Contains("start with one sentence describing the room", user, StringComparison.Ordinal);
        Assert.Contains("close-up of a defect", user, StringComparison.Ordinal);
        Assert.Contains("Room name: Bedroom 1", user, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Defect_request_asks_for_the_surface_location_size_and_the_inspector_label()
    {
        var (service, handler) = OpenAi(DefectResultJson);

        await service.AnalyzeDefectAsync(new DefectAnalysisRequest("Kitchen", "chipped worktop", [Photo], AiOutputLanguage.English));

        var user = UserText(handler.Request!);
        Assert.Contains("the surface or item it is on", user, StringComparison.Ordinal);
        Assert.Contains("approximate size", user, StringComparison.Ordinal);
        Assert.Contains("inspector's short label as the starting", user, StringComparison.Ordinal);
        Assert.Contains("Inspector's short label (may be empty): chipped worktop", user, StringComparison.Ordinal);
    }

    [Fact]
    public async Task System_message_asks_for_the_inspector_voice_and_carries_the_pt_br_terminology()
    {
        var (service, handler) = OpenAi(DefectResultJson);

        await service.AnalyzeDefectAsync(new DefectAnalysisRequest("Cozinha", "lascado", [Photo], AiOutputLanguage.PortugueseBrazil));

        var system = SystemText(handler.Request!);
        Assert.Contains("never refer to yourself, to AI, or to \"the", system, StringComparison.Ordinal);
        Assert.Contains("Write **as the inspector who visited the property**", system, StringComparison.Ordinal);
        Assert.Contains("Brazilian Portuguese terminology", system, StringComparison.Ordinal);
        // The v2 safety example taught the picture-describing voice the report must not have.
        Assert.DoesNotContain("No visible damage is apparent in the provided images", system, StringComparison.Ordinal);
    }

    [Fact]
    public void Guide_model_section_stays_within_the_teams_word_budget()
    {
        // The whole model section is sent with every call (guide header, rule 7): keep it under ~2,500 words.
        var words = InspectionWritingGuide.Current.ModelText.Split((char[])[' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(words <= 2500, $"Writing guide model section has {words} words; the budget is 2,500.");
    }

    [Fact]
    public void Mock_room_draft_follows_the_report_order_without_mentioning_photos()
    {
        var floor = new SurfaceObservation("beige", "carpet", "fair", null);

        var text = MockImageAnalysisService.MockRoomDescription("Bedroom 1", floor, ["wardrobe", "curtains"]);

        Assert.StartsWith("Bedroom 1, furnished.", text, StringComparison.Ordinal);
        Assert.True(text.IndexOf("ceiling", StringComparison.Ordinal) < text.IndexOf("Walls", StringComparison.Ordinal), text);
        Assert.True(text.IndexOf("Walls", StringComparison.Ordinal) < text.IndexOf("carpet floor", StringComparison.Ordinal), text);
        Assert.Contains("Beige carpet floor, with light wear in places.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("photo", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("image", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mock_defect_draft_starts_from_the_inspector_label_and_invents_no_position()
    {
        var text = MockImageAnalysisService.MockDefectDescription("chipped tile", "Kitchen");

        Assert.Equal("Chipped tile noted in the Kitchen. Exact size and position to be confirmed by the inspector.", text);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using InspectFlow.Modules.AI.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.AI;

/// <summary>
/// <see cref="IImageAnalysisService"/> backed by the OpenAI Chat Completions API with vision input and
/// strict JSON-schema structured output. The API key is attached per request and never logged.
/// </summary>
public sealed class OpenAiImageAnalysisService(HttpClient http, IOptions<AiOptions> options, ILogger<OpenAiImageAnalysisService> logger)
    : IImageAnalysisService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private OpenAiOptions O => options.Value.OpenAI;

    public string ProviderName => "openai";
    public string? Model => O.Model;
    public bool IsMock => false;

    public async Task<RoomAnalysisResult> AnalyzeRoomAsync(RoomAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var text = $"{AiPrompts.RoomInstruction}\nRoom name: {request.RoomName}\nRoom type (as configured): {request.RoomType}\nNumber of photos: {request.Images.Count}";
        return await CallAsync<RoomAnalysisResult>("room_analysis", AnalysisSchemas.Room(), text, [.. request.Images], request.Language, cancellationToken);
    }

    public async Task<DefectAnalysisResult> AnalyzeDefectAsync(DefectAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var text = $"{AiPrompts.DefectInstruction}\nRoom: {request.RoomName}\nInspector's short label (may be empty): {request.AgentHint}";
        var result = await CallAsync<DefectAnalysisResult>("defect_analysis", AnalysisSchemas.Defect(), text, [.. request.Images], request.Language, cancellationToken);
        return result with { Confidence = Math.Clamp(result.Confidence, 0, 1) };
    }

    public async Task<ComparisonAnalysisResult> CompareRoomAsync(RoomComparisonRequest request, CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder()
            .AppendLine(AiPrompts.ComparisonInstruction)
            .AppendLine($"Room: {request.RoomName}")
            .AppendLine($"BASELINE description: {request.BaselineDescription ?? "(none)"}")
            .AppendLine($"BASELINE recorded defects: {(request.BaselineDefects.Count == 0 ? "none" : string.Join("; ", request.BaselineDefects))}")
            .AppendLine($"CURRENT description: {request.CurrentDescription ?? "(none)"}")
            .AppendLine($"CURRENT recorded defects: {(request.CurrentDefects.Count == 0 ? "none" : string.Join("; ", request.CurrentDefects))}")
            .AppendLine($"The first {request.BaselineImages.Count} image(s) are BASELINE photos; the remaining {request.CurrentImages.Count} are CURRENT photos.")
            .ToString();
        var result = await CallAsync<ComparisonAnalysisResult>("room_comparison", AnalysisSchemas.Comparison(), text,
            [.. request.BaselineImages, .. request.CurrentImages], request.Language, cancellationToken);
        return result with { Confidence = Math.Clamp(result.Confidence, 0, 1) };
    }

    private async Task<T> CallAsync<T>(string schemaName, JsonObject schema, string userText, List<AnalysisImage> images, AiOutputLanguage language, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(O.ApiKey)) throw new AiProviderException(AiErrorTexts.NotConfigured);

        var content = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = userText } };
        foreach (var image in images)
        {
            content.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject
                {
                    ["url"] = $"data:{image.MimeType};base64,{Convert.ToBase64String(image.Content)}",
                    ["detail"] = O.ImageDetail,
                },
            });
        }

        var body = new JsonObject
        {
            ["model"] = O.Model,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = AiPrompts.SystemPrompt(language) },
                new JsonObject { ["role"] = "user", ["content"] = content },
            },
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject { ["name"] = schemaName, ["strict"] = true, ["schema"] = schema },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(O.BaseUrl), "chat/completions"))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", O.ApiKey);

        HttpResponseMessage response;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(O.TimeoutSeconds));
            response = await http.SendAsync(request, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderException(AiErrorTexts.TimedOut);
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderException(AiErrorTexts.Unreachable, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Log status only — response bodies can echo request data.
                logger.LogWarning("OpenAI request failed with status {Status}", (int)response.StatusCode);
                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new AiProviderException(AiErrorTexts.CredentialsRejected),
                    HttpStatusCode.TooManyRequests => new AiProviderException(AiErrorTexts.RateLimited),
                    HttpStatusCode.BadRequest => new AiProviderException(AiErrorTexts.CouldNotProcess),
                    _ => new AiProviderException(AiErrorTexts.ProviderError),
                };
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
            if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind == JsonValueKind.String)
                throw new AiProviderException(AiErrorTexts.Declined);
            var json = message.GetProperty("content").GetString() ?? throw new AiProviderException(AiErrorTexts.EmptyResponse);
            try
            {
                return JsonSerializer.Deserialize<T>(json, Json) ?? throw new AiProviderException(AiErrorTexts.EmptyResponse);
            }
            catch (JsonException ex)
            {
                throw new AiProviderException(AiErrorTexts.UnexpectedFormat, ex);
            }
        }
    }
}

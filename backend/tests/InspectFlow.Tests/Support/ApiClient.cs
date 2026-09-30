using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace InspectFlow.Tests.Support;

/// <summary>Thin JSON client. Responses are returned as JsonNode to keep tests close to the wire contract.</summary>
public sealed class ApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public HttpClient Http => http;
    public Guid UserId { get; private set; }
    public string? RefreshToken { get; private set; }

    public void UseToken(string? token) =>
        http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    public async Task<ApiClient> RegisterAsync(string role, string? email = null, string? name = null)
    {
        email ??= $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        var res = await PostAsync("/api/auth/register", new { email, password = TestData.Password, fullName = name ?? $"Test {role}", role });
        res.EnsureOk();
        UseToken(res.Body!["accessToken"]!.GetValue<string>());
        UserId = Guid.Parse(res.Body!["user"]!["id"]!.GetValue<string>());
        RefreshToken = res.Body!["refreshToken"]?.GetValue<string>();
        Email = email;
        return this;
    }

    public string Email { get; private set; } = string.Empty;

    public Task<ApiResponse> GetAsync(string url) => SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
    public Task<ApiResponse> DeleteAsync(string url) => SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));
    public Task<ApiResponse> PostAsync(string url, object? body = null) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body ?? new { }, options: Json) });
    public Task<ApiResponse> PutAsync(string url, object body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body, options: Json) });

    public Task<ApiResponse> UploadAsync(string url, byte[] content, string fileName, string contentType, string mediaType = "General", Guid? defectId = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(mediaType), "mediaType");
        if (defectId is not null) form.Add(new StringContent(defectId.Value.ToString()), "defectId");
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = form });
    }

    public async Task<ApiResponse> SendAsync(HttpRequestMessage request)
    {
        using var response = await http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        JsonNode? body = null;
        if (!string.IsNullOrWhiteSpace(text) && (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.Ordinal) ?? false))
            body = JsonNode.Parse(text);
        return new ApiResponse(response.StatusCode, body, text);
    }
}

public sealed record ApiResponse(HttpStatusCode Status, JsonNode? Body, string Raw)
{
    public ApiResponse EnsureOk()
    {
        if ((int)Status is < 200 or > 299)
            throw new Xunit.Sdk.XunitException($"Expected success but got {(int)Status}: {Raw}");
        return this;
    }

    public ApiResponse EnsureStatus(HttpStatusCode expected)
    {
        if (Status != expected)
            throw new Xunit.Sdk.XunitException($"Expected {(int)expected} but got {(int)Status}: {Raw}");
        return this;
    }

    public string? Code => Body?["code"]?.GetValue<string>();
    public JsonNode this[string key] => Body![key]!;
    public Guid Id => Guid.Parse(Body!["id"]!.GetValue<string>());
}

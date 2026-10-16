using System.Net;
using System.Net.Http.Json;
using InspectFlow.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InspectFlow.Tests.Integration;

/// <summary>
/// The "if_session" hint lets the web server route "/" (landing page vs dashboard) without seeing the refresh token.
/// It must follow the session: set on sign-in and refresh, removed on logout and on a failed refresh (ADR 0018).
/// </summary>
[Collection(ApiCollection.Name)]
public class SessionHintCookieTests(TestApp app)
{
    private HttpClient RawClient() => app.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static HttpRequestMessage WebPost(string url, object body, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Client", "web");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return request;
    }

    private static string? SetCookie(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith($"{name}=", StringComparison.Ordinal))
            : null;

    private async Task<(HttpClient Http, string RefreshCookie)> SignInAsync()
    {
        var registered = await app.CreateClient().RegisterAsync("Agent");
        var http = RawClient();
        using var login = await http.SendAsync(WebPost("/api/auth/login", new { email = registered.Email, password = TestData.Password }));
        login.EnsureSuccessStatusCode();
        var refresh = SetCookie(login, "if_refresh") ?? throw new Xunit.Sdk.XunitException("Login set no if_refresh cookie.");
        return (http, refresh.Split(';')[0]);
    }

    [Fact]
    public async Task Sign_in_sets_a_site_wide_http_only_hint_with_no_secret()
    {
        var registered = await app.CreateClient().RegisterAsync("Tenant");
        using var login = await RawClient().SendAsync(WebPost("/api/auth/login", new { email = registered.Email, password = TestData.Password }));

        var hint = SetCookie(login, "if_session") ?? throw new Xunit.Sdk.XunitException("Login set no if_session cookie.");
        Assert.StartsWith("if_session=1;", hint, StringComparison.Ordinal);
        Assert.Contains("path=/;", hint + ";", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_renews_the_hint_and_logout_removes_it()
    {
        var (http, refreshCookie) = await SignInAsync();
        using var refreshed = await http.SendAsync(WebPost("/api/auth/refresh", new { }, refreshCookie));
        refreshed.EnsureSuccessStatusCode();
        Assert.StartsWith("if_session=1;", SetCookie(refreshed, "if_session"), StringComparison.Ordinal);

        var rotated = SetCookie(refreshed, "if_refresh")!.Split(';')[0];
        using var logout = await http.SendAsync(WebPost("/api/auth/logout", new { }, rotated));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", SetCookie(logout, "if_session"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Token_rotated_by_a_parallel_tab_keeps_the_hint_because_the_session_is_alive()
    {
        var (http, refreshCookie) = await SignInAsync();
        using var first = await http.SendAsync(WebPost("/api/auth/refresh", new { }, refreshCookie));
        first.EnsureSuccessStatusCode();

        // A second tab still holding the old token, within the rotation grace window.
        using var second = await http.SendAsync(WebPost("/api/auth/refresh", new { }, refreshCookie));
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Contains("auth.refresh_superseded", await second.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Null(SetCookie(second, "if_session"));
    }

    [Fact]
    public async Task Failed_refresh_removes_the_hint_so_the_landing_page_is_shown_again()
    {
        using var failed = await RawClient().SendAsync(WebPost("/api/auth/refresh", new { }, "if_refresh=not-a-valid-token; if_session=1"));

        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", SetCookie(failed, "if_session"), StringComparison.OrdinalIgnoreCase);
    }
}

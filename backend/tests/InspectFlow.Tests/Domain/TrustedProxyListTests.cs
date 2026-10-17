using System.Net;
using InspectFlow.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace InspectFlow.Tests.Domain;

/// <summary>TRUSTED_PROXIES parsing and its effect on ForwardedHeadersMiddleware (ADR 0024).</summary>
public class TrustedProxyListTests
{
    private const string CaddyIp = "172.30.250.10";

    [Fact]
    public void Parses_addresses_and_cidr_ranges()
    {
        var list = TrustedProxyList.Parse($" {CaddyIp}, 2001:db8::1 ,10.0.0.0/8, fd00::/64 ");

        Assert.Equal([IPAddress.Parse(CaddyIp), IPAddress.Parse("2001:db8::1")], list.Addresses);
        Assert.Equal([IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("fd00::/64")], list.Networks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ")]
    public void Empty_list_is_refused(string? value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => TrustedProxyList.Parse(value));
        Assert.Contains("requires TRUSTED_PROXIES", error.Message);
        Assert.Contains("CIDR", error.Message);
    }

    [Theory]
    [InlineData("web")]
    [InlineData("172.30.250")]
    [InlineData("300.1.1.1")]
    [InlineData("10.0.0.0/x")]
    [InlineData("10.0.0.0/33")]
    public void Invalid_entry_is_refused_with_the_offending_value(string entry)
    {
        var error = Assert.Throws<InvalidOperationException>(() => TrustedProxyList.Parse($"{CaddyIp},{entry}"));
        Assert.Contains($"'{entry}'", error.Message);
        Assert.Contains("e.g.", error.Message);
    }

    [Fact]
    public async Task Client_ip_comes_from_a_trusted_proxy()
    {
        var context = await ForwardAsync(remote: CaddyIp, forwardedFor: "203.0.113.7");
        Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task Ipv4_mapped_address_of_the_trusted_proxy_is_recognised()
    {
        // Kestrel listening on "+" accepts IPv4 connections on a dual-mode socket and reports them as ::ffff:a.b.c.d.
        var context = await ForwardAsync(remote: "::ffff:" + CaddyIp, forwardedFor: "203.0.113.7");
        Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
    }

    [Theory]
    [InlineData("172.30.250.20")] // the web container: Next.js passes the client's own X-Forwarded-For through
    [InlineData("127.0.0.1")] // loopback is no longer trusted by default
    public async Task Forwarded_for_from_any_other_sender_is_ignored(string remote)
    {
        var context = await ForwardAsync(remote, forwardedFor: "6.6.6.6");
        Assert.Equal(IPAddress.Parse(remote), context.Connection.RemoteIpAddress);
    }

    private static async Task<HttpContext> ForwardAsync(string remote, string forwardedFor)
    {
        var options = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
        TrustedProxyList.Parse(CaddyIp).ApplyTo(options);
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        await middleware.Invoke(context);
        return context;
    }
}

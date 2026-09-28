using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class CsrfEndpointTests : IDisposable
{
    private const string AllowedOrigin = "http://localhost:3000";

    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public CsrfEndpointTests(PostgresFixture postgres)
    {
        _factory = new ReceptionApiFactory(postgres);
        _http = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    public void Dispose()
    {
        _http.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task A_cookie_bearing_write_without_the_header_is_403()
    {
        var response = await _http.SendAsync(SignOut(cookie: "ms_access_token=x", origin: AllowedOrigin, csrfHeader: null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("csrf.header_missing", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_cookie_bearing_write_from_a_foreign_origin_is_403()
    {
        var response = await _http.SendAsync(SignOut("ms_access_token=x", "https://evil.example", "1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("csrf.origin_not_allowed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_cookie_bearing_write_that_names_no_origin_is_403()
    {
        var response = await _http.SendAsync(SignOut("ms_access_token=x", origin: null, csrfHeader: "1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("csrf.origin_not_allowed", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("http://localhost:3000.evil.com")]
    [InlineData("http://localhost:3000@evil.com")]
    [InlineData("https://localhost:3000")]
    [InlineData("http://localhost:3001")]
    public async Task A_lookalike_origin_is_refused_even_with_the_header(string origin)
    {
        var response = await _http.SendAsync(SignOut("ms_access_token=x", origin, "1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("csrf.origin_not_allowed", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("http://localhost:3000/")]
    [InlineData("HTTP://LOCALHOST:3000")]
    public async Task A_differently_written_allowed_origin_is_accepted(string origin)
    {
        var response = await _http.SendAsync(SignOut("ms_access_token=x", origin, "1"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_cookie_bearing_write_from_the_allowed_origin_with_the_header_passes()
    {
        var response = await _http.SendAsync(SignOut("ms_access_token=x", AllowedOrigin, "1"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Signing_in_from_a_fresh_browser_needs_no_header()
    {
        var response = await _http.PostAsync("/api/identity/auth/sign-in", ApiJson.Credentials("magasinier", "Reception2026"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_bearer_client_without_a_cookie_is_not_subject_to_the_guard()
    {
        var response = await _factory.CreateAuthenticatedClient().PostAsync("/api/identity/auth/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpRequestMessage SignOut(string cookie, string? origin, string? csrfHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/sign-out");
        request.Headers.Add("Cookie", cookie);
        if (origin is not null) request.Headers.Add("Origin", origin);
        if (csrfHeader is not null) request.Headers.Add("X-MS-CSRF", csrfHeader);

        return request;
    }
}

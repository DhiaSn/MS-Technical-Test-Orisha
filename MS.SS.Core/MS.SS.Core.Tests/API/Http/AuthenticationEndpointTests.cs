using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class AuthenticationEndpointTests : IDisposable
{
    private const string SignIn = "/api/identity/auth/sign-in";
    private const string Me = "/api/identity/auth/me";
    private const string CookieName = "ms_access_token=";

    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public AuthenticationEndpointTests(PostgresFixture postgres)
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
    public async Task Signing_in_sets_an_HttpOnly_cookie_and_returns_the_session_without_the_token()
    {
        var response = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Reception2026"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(CookieName));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        var token = cookie[CookieName.Length..cookie.IndexOf(';')];
        Assert.DoesNotContain(token, body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("magasinier", json.RootElement.GetProperty("username").GetString());
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task The_token_is_in_no_header_but_the_cookie()
    {
        var response = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Reception2026"));

        var token = CookieHeaderOf(response)[CookieName.Length..];
        foreach (var header in response.Headers.Concat(response.Content.Headers).Where(h => h.Key != "Set-Cookie"))
        {
            Assert.DoesNotContain(token, string.Join(",", header.Value));
        }
    }

    [Fact]
    public async Task Wrong_password_and_unknown_user_answer_the_same_401()
    {
        var wrong = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Wrong-Password1"));
        var unknown = await _http.PostAsync(SignIn, ApiJson.Credentials("nobody", "Reception2026"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await Stripped(wrong), await Stripped(unknown));
        Assert.Contains("auth.invalid_credentials", await wrong.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_cookie_from_sign_in_opens_the_session_at_me()
    {
        var signIn = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Reception2026"));

        var me = await _http.SendAsync(WithCookie(HttpMethod.Get, Me, CookieHeaderOf(signIn)));

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains("\"username\":\"magasinier\"", await me.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_reports_the_expiry_of_the_token_behind_the_cookie()
    {
        var signIn = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Reception2026"));
        var cookie = CookieHeaderOf(signIn);
        var expected = new JwtSecurityTokenHandler().ReadJwtToken(cookie[CookieName.Length..]).ValidTo;

        var me = await _http.SendAsync(WithCookie(HttpMethod.Get, Me, cookie));

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var json = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var reported = json.RootElement.GetProperty("accessTokenExpiresAt").GetDateTimeOffset().UtcDateTime;
        Assert.Equal(DateTime.SpecifyKind(expected, DateTimeKind.Utc), reported);
    }

    [Fact]
    public async Task Me_without_a_session_is_a_401_problem_with_a_stable_code()
    {
        var response = await _http.GetAsync(Me);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("auth.unauthenticated", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_invalid_bearer_header_is_not_rescued_by_a_valid_cookie()
    {
        var signIn = await _http.PostAsync(SignIn, ApiJson.Credentials("magasinier", "Reception2026"));
        var request = WithCookie(HttpMethod.Get, Me, CookieHeaderOf(signIn));
        request.Headers.Authorization = new("Bearer", "not.a.jwt");

        var response = await _http.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signing_out_expires_the_cookie()
    {
        var response = await _http.PostAsync("/api/identity/auth/sign-out", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(CookieName));
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registering_creates_an_operator_and_opens_a_session()
    {
        var name = $"op-{Guid.NewGuid():N}"[..20];

        var response = await Register(name, "Nouvel Opérateur", "Reception2026");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(CookieName));
        var again = await _http.PostAsync(SignIn, ApiJson.Credentials(name, "Reception2026"));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task A_taken_username_is_409_and_a_weak_password_lists_every_broken_rule()
    {
        var taken = await Register("magasinier", "Name", "Reception2026");
        var weak = await Register($"weak-{Guid.NewGuid():N}"[..15], "Name", "abc");

        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Contains("account.username_taken", await taken.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        var body = await weak.Content.ReadAsStringAsync();
        Assert.Contains("password.too_short", body);
        Assert.Contains("password.missing_uppercase", body);
        Assert.Contains("password.missing_digit", body);
    }

    [Fact]
    public async Task Simultaneous_identical_registrations_give_one_201_and_one_409_never_a_500()
    {
        // The existence check and the insert are not atomic, so the loser is only decided by the unique
        // index when both requests get past the check. Repeating the race makes that window likely to be hit.
        for (var round = 0; round < 5; round++)
        {
            var name = $"race-{Guid.NewGuid():N}"[..18];

            var responses = await Task.WhenAll(
                Register(name, "A", "Reception2026"), Register(name, "B", "Reception2026"));

            Assert.Equal(
                new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
                responses.Select(r => r.StatusCode).Order());
            var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Contains("account.username_taken", await loser.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task The_password_policy_is_public()
    {
        var response = await _http.GetAsync("/api/identity/auth/password-policy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"minimumLength\":8", await response.Content.ReadAsStringAsync());
    }

    private static HttpRequestMessage WithCookie(HttpMethod method, string url, string cookie)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", cookie);

        return request;
    }

    private static string CookieHeaderOf(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(CookieName)).Split(';')[0];

    private static async Task<string> Stripped(HttpResponseMessage response)
    {
        var node = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        node.Remove("traceId");

        return node.ToJsonString();
    }

    private Task<HttpResponseMessage> Register(string username, string displayName, string password) =>
        _http.PostAsync("/api/identity/auth/register",
            ApiJson.Body(JsonSerializer.Serialize(new { username, displayName, password })));
}

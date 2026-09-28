using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class SessionValidationEndpointTests : IDisposable
{
    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public SessionValidationEndpointTests(PostgresFixture postgres)
    {
        _factory = new ReceptionApiFactory(postgres);
        _http = _factory.CreateClient();
    }

    public void Dispose()
    {
        _http.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task A_valid_token_for_a_user_that_does_not_exist_is_a_401_at_me() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync(Mint()));

    [Fact]
    public async Task An_expired_token_is_401() =>
        await AssertUnauthenticatedAsync(
            await MeWithBearerAsync(Mint(expires: DateTime.UtcNow.AddMinutes(-1), notBefore: DateTime.UtcNow.AddMinutes(-10))));

    [Fact]
    public async Task A_tampered_token_is_401() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync(Mint()[..^3] + "abc"));

    [Fact]
    public async Task A_wrong_audience_is_401() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync(Mint(audience: "someone-else")));

    [Fact]
    public async Task A_wrong_issuer_is_401() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync(Mint(issuer: "someone-else")));

    [Fact]
    public async Task A_token_signed_with_another_key_is_401() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync(Mint(secret: new string('z', 48))));

    [Fact]
    public async Task Garbage_is_401_not_500() =>
        await AssertUnauthenticatedAsync(await MeWithBearerAsync("not.a.jwt"));

    [Fact]
    public async Task A_garbage_cookie_is_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/auth/me");
        request.Headers.Add("Cookie", "ms_access_token=garbage");

        await AssertUnauthenticatedAsync(await _http.SendAsync(request));
    }

    [Fact]
    public async Task Health_and_the_api_documents_stay_public()
    {
        Assert.Equal(HttpStatusCode.OK, (await _http.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _http.GetAsync("/openapi/v1.json")).StatusCode);
    }

    private Task<HttpResponseMessage> MeWithBearerAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/auth/me");
        request.Headers.Authorization = new("Bearer", token);

        return _http.SendAsync(request);
    }

    private static async Task AssertUnauthenticatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("auth.unauthenticated", body.RootElement.GetProperty("code").GetString());
    }

    // Same claims as TokenService, signed the way the API signs, each ingredient overridable.
    private static string Mint(
        string issuer = "ms.ss.core", string audience = "ms.ca.clientapp", string? secret = null,
        DateTime? expires = null, DateTime? notBefore = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? new string('k', 48)));
        var userId = Guid.NewGuid().ToString();
        var jwt = new JwtSecurityToken(
            issuer, audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(MsClaims.Role, IdentityRoles.Operator)
            ],
            notBefore ?? DateTime.UtcNow.AddSeconds(-5), expires ?? DateTime.UtcNow.AddHours(1),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

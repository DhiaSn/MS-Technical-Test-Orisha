using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Extensions;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;
using MS.SS.Core.Security.Services;

namespace MS.SS.Core.Tests.Security;

public class SecurityServiceExtensionsTests : IAsyncLifetime
{
    private const string Issuer = "ms.ss.core";
    private const string Audience = "ms.ca.clientapp";
    private const string Secret = "0123456789-0123456789-0123456789-01";

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TokenOptions:Issuer"] = Issuer,
            ["TokenOptions:Audience"] = Audience,
            ["TokenOptions:Secret"] = Secret
        });
        builder.Services.AddSecurityServices(builder.Configuration);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/protected", (ClaimsPrincipal user) => Results.Ok(user.GetUserId()));
        _app.MapGet("/open", () => Results.Ok("open")).AllowAnonymous();
        await _app.StartAsync();

        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task An_endpoint_with_no_annotation_is_protected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task An_endpoint_can_opt_out_with_allow_anonymous()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/open")).StatusCode);
    }

    [Fact]
    public async Task A_valid_bearer_token_is_accepted_and_identifies_the_user()
    {
        var userId = Guid.NewGuid();

        var response = await GetWithBearer(MintToken(userId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(userId.ToString(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_valid_token_in_the_access_cookie_is_accepted_and_identifies_the_user()
    {
        var userId = Guid.NewGuid();

        var response = await GetWithCookie(MintToken(userId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(userId.ToString(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_token_in_a_cookie_with_another_name_is_ignored()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Cookie", $"other_cookie={MintToken(Guid.NewGuid())}");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task The_bearer_header_wins_over_the_cookie()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", MintToken(Guid.NewGuid()));
        request.Headers.Add("Cookie", $"{SecurityConstants.AccessTokenCookie}=garbage");

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    [InlineData("..")]
    public async Task Garbage_tokens_are_rejected_from_both_carriers(string token)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithCookie(token)).StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var token = Mint(Issuer, Audience, Secret, notBefore: DateTime.UtcNow.AddMinutes(-10), expires: DateTime.UtcNow.AddSeconds(-1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(token)).StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        var token = Mint(Issuer, Audience, new string('x', 48));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(token)).StatusCode);
    }

    [Fact]
    public async Task A_token_with_a_tampered_payload_is_rejected()
    {
        var parts = MintToken(Guid.NewGuid()).Split('.');
        var payload = Base64UrlEncoder.DecodeBytes(parts[1]);
        payload[10] ^= 0x01;
        var tampered = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(tampered)).StatusCode);
    }

    [Fact]
    public async Task A_token_from_another_issuer_is_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(Mint("someone.else", Audience, Secret))).StatusCode);
    }

    [Fact]
    public async Task A_token_for_another_audience_is_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(Mint(Issuer, "another.app", Secret))).StatusCode);
    }

    [Fact]
    public async Task An_unsigned_token_is_rejected()
    {
        var jwt = new JwtSecurityToken(Issuer, Audience, [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        var header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        var unsigned = $"{header}.{jwt.EncodedPayload}.";

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithBearer(unsigned)).StatusCode);
    }

    [Fact]
    public void The_password_and_token_services_are_stateless_singletons()
    {
        var services = _app.Services;

        Assert.IsType<PasswordService>(services.GetRequiredService<IPasswordService>());
        Assert.IsType<TokenService>(services.GetRequiredService<ITokenService>());
        Assert.Same(services.GetRequiredService<IPasswordService>(), services.GetRequiredService<IPasswordService>());
        Assert.Same(services.GetRequiredService<ITokenService>(), services.GetRequiredService<ITokenService>());
        Assert.Same(services.GetRequiredService<SigningConfigurations>(), services.GetRequiredService<SigningConfigurations>());
    }

    [Fact]
    public void The_fallback_policy_requires_an_authenticated_user()
    {
        var options = _app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(options.FallbackPolicy!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void A_missing_or_short_secret_stops_the_app_from_starting_and_names_the_setting(string? secret)
    {
        var values = new Dictionary<string, string?> { ["TokenOptions:Issuer"] = Issuer, ["TokenOptions:Audience"] = Audience };
        if (secret is not null)
            values["TokenOptions:Secret"] = secret;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSecurityServices(configuration));

        Assert.Contains("TokenOptions:Secret", error.Message);
    }

    [Fact]
    public void A_missing_token_options_section_stops_the_app_from_starting()
    {
        var configuration = new ConfigurationBuilder().Build();

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSecurityServices(configuration));

        Assert.Contains("TokenOptions:Secret", error.Message);
    }

    private static string MintToken(Guid userId)
    {
        var options = Options.Create(new TokenOptions { Issuer = Issuer, Audience = Audience, Secret = Secret });
        return new TokenService(options, new SigningConfigurations(Secret))
            .CreateAccessToken(new TokenPrincipal(userId, IdentityRoles.Operator)).Token;
    }

    private static string Mint(string issuer, string audience, string secret, DateTime? notBefore = null, DateTime? expires = null)
    {
        var jwt = new JwtSecurityToken(
            issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore ?? DateTime.UtcNow, expires ?? DateTime.UtcNow.AddMinutes(5),
            new SigningConfigurations(secret).SigningCredentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private Task<HttpResponseMessage> GetWithBearer(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetWithCookie(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Cookie", $"{SecurityConstants.AccessTokenCookie}={token}");
        return _client.SendAsync(request);
    }
}

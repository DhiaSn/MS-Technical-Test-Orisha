using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;
using MS.SS.Core.Security.Services;

namespace MS.SS.Core.Tests.Security;

public class TokenServiceTests
{
    private static readonly TokenOptions Options = new()
    {
        Issuer = "ms.ss.core", Audience = "ms.ca.clientapp", Secret = new string('k', 48), AccessTokenExpirationSeconds = 60
    };

    private readonly SigningConfigurations _signing = new(Options.Secret);
    private TokenService Service => new(Microsoft.Extensions.Options.Options.Create(Options), _signing);

    [Fact]
    public void The_token_carries_the_user_id_and_role_and_expires_after_the_configured_lifetime()
    {
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var token = Service.CreateAccessToken(new TokenPrincipal(userId, IdentityRoles.Operator));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
        Assert.Equal(userId.ToString(), jwt.Subject);
        Assert.Equal(IdentityRoles.Operator, jwt.Claims.Single(c => c.Type == MsClaims.Role).Value);
        Assert.InRange(token.ExpiresAt, before.AddSeconds(59), before.AddSeconds(62));
    }

    [Fact]
    public void The_token_carries_nothing_personal_beyond_the_user_id_and_role()
    {
        var token = Service.CreateAccessToken(new TokenPrincipal(Guid.NewGuid(), IdentityRoles.Operator));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        var expected = new[] { "sub", "jti", "nbf", "exp", "iat", "iss", "aud", ClaimTypes.NameIdentifier, MsClaims.Role };
        Assert.All(jwt.Claims, c => Assert.Contains(c.Type, expected));
    }

    [Fact]
    public void Each_token_has_its_own_identifier()
    {
        var principal = new TokenPrincipal(Guid.NewGuid(), IdentityRoles.Operator);

        var first = new JwtSecurityTokenHandler().ReadJwtToken(Service.CreateAccessToken(principal).Token);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(Service.CreateAccessToken(principal).Token);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void The_token_validates_with_the_signing_key_and_not_with_another()
    {
        var token = Service.CreateAccessToken(new TokenPrincipal(Guid.NewGuid(), IdentityRoles.Operator)).Token;

        Assert.NotNull(Validate(token, _signing));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, new SigningConfigurations(new string('x', 48))));
    }

    [Fact]
    public void A_token_for_nobody_is_refused()
    {
        Assert.Throws<InvalidOperationException>(() => Service.CreateAccessToken(new TokenPrincipal(Guid.Empty, IdentityRoles.Operator)));
    }

    private static ClaimsPrincipal Validate(string token, SigningConfigurations signing) =>
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer, ValidAudience = Options.Audience, IssuerSigningKey = signing.SecurityKey,
            ClockSkew = TimeSpan.Zero
        }, out _);
}

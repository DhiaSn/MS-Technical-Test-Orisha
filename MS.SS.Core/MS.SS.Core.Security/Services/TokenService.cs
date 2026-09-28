using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;

namespace MS.SS.Core.Security.Services;

public sealed class TokenService(IOptions<TokenOptions> tokenOptions, SigningConfigurations signingConfigurations) : ITokenService
{
    public AccessToken CreateAccessToken(TokenPrincipal principal)
    {
        if (principal.UserId == Guid.Empty)
            throw new InvalidOperationException("Refusing to mint an access token without a user.");

        var options = tokenOptions.Value;
        var now = DateTime.UtcNow;
        var expiresAt = now.AddSeconds(options.AccessTokenExpirationSeconds);

        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, principal.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, principal.UserId.ToString()),
                new Claim(MsClaims.Role, principal.Role)
            ],
            notBefore: now,
            expires: expiresAt,
            signingCredentials: signingConfigurations.SigningCredentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}

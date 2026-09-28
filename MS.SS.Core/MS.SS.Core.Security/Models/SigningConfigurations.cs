using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MS.SS.Core.Security.Models;

/// <summary>
/// Holds the symmetric signing key. Registered as a singleton so the key is derived once.
/// </summary>
public sealed class SigningConfigurations
{
    /// <summary>
    /// HMAC-SHA256 needs at least 256 bits of key material; a shorter secret makes the token
    /// forgeable, so it is rejected at startup rather than at first sign-in.
    /// </summary>
    private const int MinimumSecretBytes = 32;

    public SigningConfigurations(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "TokenOptions:Secret is not configured. Set it via user-secrets locally or the deployment secret store.");

        var keyBytes = Encoding.UTF8.GetBytes(secret);

        if (keyBytes.Length < MinimumSecretBytes)
            throw new InvalidOperationException(
                $"TokenOptions:Secret must be at least {MinimumSecretBytes} bytes for HMAC-SHA256 signing.");

        SecurityKey = new SymmetricSecurityKey(keyBytes);
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    }

    public SymmetricSecurityKey SecurityKey { get; }

    public SigningCredentials SigningCredentials { get; }
}

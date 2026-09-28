namespace MS.SS.Core.Security.Models.Tokens;

public sealed class TokenOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in seconds. Default 8 hours, one shift.</summary>
    public int AccessTokenExpirationSeconds { get; set; } = 28_800;
}

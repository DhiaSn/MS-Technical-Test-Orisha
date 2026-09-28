namespace MS.SS.Core.Security.Config;

public static class SecurityConstants
{
    /// <summary>Cookie the SPA receives the access token in; also read by the JWT bearer handler.</summary>
    public const string AccessTokenCookie = "ms_access_token";

    /// <summary>
    /// PBKDF2 work factor. OWASP's 2023 guidance for PBKDF2-HMAC-SHA256 is 600,000 iterations;
    /// that is what we use. Raising it later is safe — the iteration count is written into every
    /// stored hash, so old hashes keep verifying with the count they were created under.
    /// </summary>
    public const int PasswordIterations = 600_000;
}

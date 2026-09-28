using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Models.Tokens;

namespace MS.SS.Core.API.Extensions;

public static class AuthCookieExtensions
{
    public static void WriteAuthCookie(this HttpContext context, AccessToken token, bool isDevelopment)
    {
        var options = CookieOptionsFor(isDevelopment);
        options.Expires = token.ExpiresAt;

        context.Response.Cookies.Append(SecurityConstants.AccessTokenCookie, token.Token, options);
    }

    // Deleted with the attributes it was written with, so a Secure cookie is reliably removed over HTTPS.
    public static void ClearAuthCookie(this HttpContext context, bool isDevelopment) =>
        context.Response.Cookies.Delete(SecurityConstants.AccessTokenCookie, CookieOptionsFor(isDevelopment));

    // Lax rather than None: the browser only talks to its own origin, so the cookie never has to
    // travel cross-site. Secure is dropped in Development only, so plain http://localhost works.
    private static CookieOptions CookieOptionsFor(bool isDevelopment) => new()
    {
        HttpOnly = true,
        Secure = !isDevelopment,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };
}

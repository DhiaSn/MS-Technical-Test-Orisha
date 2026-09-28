using MS.SS.Core.App.Http;
using MS.SS.Core.Security.Config;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.API.Middlewares;

/// <summary>
/// Blocks cross-site request forgery against the cookie session.
/// </summary>
/// <remarks>
/// The browser attaches the session cookie to requests fired from any site, so two checks make a
/// forged write fail:
/// <list type="number">
/// <item>The <c>Origin</c> header (or, when a browser omits it, the <c>Referer</c>) must be the
/// API's own origin or one listed in <c>AllowedOrigins</c>.</item>
/// <item>A custom <see cref="HeaderName"/> header must be present. A page on another origin cannot
/// add a custom header without a CORS preflight, and the preflight is only granted to the origins
/// in <c>AllowedOrigins</c>.</item>
/// </list>
/// Only unsafe methods that carry the session cookie are checked: a request without it has no
/// session to abuse, which keeps sign-in from a fresh browser and Bearer clients unaffected. An empty
/// <c>AllowedOrigins</c> therefore rejects every cookie-bearing write, failing closed.
/// </remarks>
public sealed class CsrfProtectionMiddleware
{
    public const string HeaderName = "X-MS-CSRF";

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace
    };

    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedOrigins = (configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
            .Select(NormalizeOrigin)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method) || !CarriesSession(context.Request))
        {
            await _next(context);
            return;
        }

        if (!IsAllowedOrigin(context.Request))
        {
            _logger.LogWarning(
                "CSRF guard refused {Method} {Path}: origin {Origin} is not allowed",
                context.Request.Method, context.Request.Path, RequestOrigin(context.Request) ?? "(none)");

            await ProblemResponses.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                ErrorCodes.CsrfOriginNotAllowed,
                "Request origin not allowed.");
            return;
        }

        if (string.IsNullOrWhiteSpace(context.Request.Headers[HeaderName]))
        {
            _logger.LogWarning(
                "CSRF guard refused {Method} {Path}: {Header} header missing",
                context.Request.Method, context.Request.Path, HeaderName);

            await ProblemResponses.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                ErrorCodes.CsrfHeaderMissing,
                "Missing CSRF protection header.");
            return;
        }

        await _next(context);
    }

    private static bool CarriesSession(HttpRequest request) =>
        request.Cookies.ContainsKey(SecurityConstants.AccessTokenCookie);

    private bool IsAllowedOrigin(HttpRequest request)
    {
        var origin = RequestOrigin(request);

        if (origin is null) return false;

        return _allowedOrigins.Contains(origin)
               || string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
    }

    private static string? RequestOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();

        // "null" is what a sandboxed or redirected context sends; it fails to parse and identifies nobody.
        if (!string.IsNullOrEmpty(origin)) return NormalizeOrigin(origin);

        var referer = request.Headers.Referer.ToString();

        return string.IsNullOrEmpty(referer) ? null : NormalizeOrigin(referer);
    }

    private static string? NormalizeOrigin(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant()
            : null;
}

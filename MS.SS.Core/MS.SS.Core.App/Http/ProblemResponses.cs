using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MS.SS.Core.App.Http;

/// <summary>
/// Writes an RFC 9457 problem body carrying the stable <c>code</c> extension, for the middleware
/// that has to answer before an endpoint (and so <c>Results.Problem</c>) is reached.
/// </summary>
public static class ProblemResponses
{
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        IDictionary<string, object?>? extensions = null,
        CancellationToken cancellationToken = default)
    {
        var problem = new ProblemDetails { Status = statusCode, Title = title };
        problem.Extensions["code"] = code;

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions) problem.Extensions[key] = value;
        }

        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);
    }
}

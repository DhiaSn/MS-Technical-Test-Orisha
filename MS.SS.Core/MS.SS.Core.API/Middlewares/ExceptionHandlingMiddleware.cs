using System.Diagnostics;
using MS.SS.Core.API.Extensions;

namespace MS.SS.Core.API.Middlewares;

/// <summary>
/// Last-resort handler for exceptions that escape an endpoint.
/// </summary>
/// <remarks>
/// Handlers return failures as <c>Result</c>; anything reaching here is either a genuine bug or a
/// thrown failure such as a body that cannot be bound. The response is produced by
/// <see cref="ResultExtensions.ToProblem"/>, so the status mapping is identical whether a failure
/// was returned or thrown.
/// </remarks>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller went away mid-request: nobody is left to answer and it is not a fault.
            logger.LogDebug(
                "Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Exception after the response started; cannot rewrite it.");
                throw;
            }

            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            if (ex is BadHttpRequestException)
            {
                logger.LogWarning(
                    "Bad request on {Method} {Path}: {Reason}. TraceId={TraceId}",
                    context.Request.Method, context.Request.Path, ex.Message, traceId);
            }
            else
            {
                logger.LogError(ex,
                    "Unhandled exception on {Method} {Path}. TraceId={TraceId}",
                    context.Request.Method, context.Request.Path, traceId);
            }

            await ResultExtensions.ToProblem(ex).ExecuteAsync(context);
        }
    }
}

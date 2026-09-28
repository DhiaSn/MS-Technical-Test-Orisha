using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using MS.SS.Core.App.Http;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.API.Middlewares;

/// <summary>
/// Gives the framework's bodiless 401 and 403 a problem body with a stable code, so the client can
/// tell "session expired" apart from "not allowed" without inspecting headers.
/// </summary>
public sealed class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // The default handler still issues the challenge or forbid (status and WWW-Authenticate);
        // the body is only added on top of it.
        await _default.HandleAsync(next, context, policy, authorizeResult);

        if (context.Response.HasStarted) return;

        if (authorizeResult.Challenged)
        {
            await ProblemResponses.WriteAsync(
                context, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Not authenticated.");
        }
        else if (authorizeResult.Forbidden)
        {
            await ProblemResponses.WriteAsync(
                context, StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "Access denied.");
        }
    }
}

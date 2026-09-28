using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MS.SS.Core.API.Extensions;
using MS.SS.Core.App.Extensions;
using MS.SS.Core.Modules.Identity.Application.Commands.AuthenticationCommands;
using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Models;
using MS.SS.Core.Modules.Identity.Application.Queries.UserQueries;
using MS.SS.Core.Security.Extensions;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Results;
using Wolverine;

namespace MS.SS.Core.API.Endpoints.Identity;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/auth").WithTags("Identity - Authentication");

        MapSignIn(group);
        MapRegister(group);
        MapSignOut(group);
        MapMe(group);
        MapPasswordPolicy(group);

        return app;
    }

    private static void MapSignIn(RouteGroupBuilder group)
    {
        group.MapPost("/sign-in", async (
            [FromBody] SignInRequest request, IMessageBus bus, HttpContext http, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<AuthenticationResult>>(new SignInCommand(request), ct);

            return result.ToHttpResult(value =>
            {
                http.WriteAuthCookie(value.AccessToken, env.IsDevelopment());
                return Results.Ok(value.Response);
            });
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimiterExtensions.AuthPolicy)
        .WithName("SignIn")
        .WithSummary("Sign in")
        .WithDescription(
            "Opens the session: the access token is set in an HttpOnly cookie and is never in the body. " +
            "Every failure answers the same 401 auth.invalid_credentials, whatever the cause.")
        .Produces<SessionResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);
    }

    private static void MapRegister(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request, IMessageBus bus, HttpContext http, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var result = await bus.InvokeAsync<Result<AuthenticationResult>>(new RegisterOperatorCommand(request), ct);

            return result.ToHttpResult(value =>
            {
                http.WriteAuthCookie(value.AccessToken, env.IsDevelopment());
                return Results.Created($"/api/identity/users/{value.Response.UserId}", value.Response);
            });
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimiterExtensions.AuthPolicy)
        .WithName("RegisterOperator")
        .WithSummary("Create an operator account")
        .WithDescription("Creates an Operator and opens the session. 409 account.username_taken, 400 with per-field codes.")
        .Produces<SessionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);
    }

    private static void MapSignOut(RouteGroupBuilder group)
    {
        group.MapPost("/sign-out", (HttpContext http, IWebHostEnvironment env) =>
        {
            http.ClearAuthCookie(env.IsDevelopment());
            return Results.NoContent();
        })
        .AllowAnonymous()
        .WithName("SignOut")
        .WithSummary("Sign out")
        .WithDescription("Clears the session cookie. The token itself stays valid until it expires.")
        .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapMe(RouteGroupBuilder group)
    {
        group.MapGet("/me", async (ClaimsPrincipal principal, IMessageBus bus, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            if (userId is null) return ResultExtensions.ToProblem(new UnauthenticatedException("Not authenticated."));

            // Every token this API mints or accepts carries exp, so the claim is always present.
            var expiry = DateTimeOffset
                .FromUnixTimeSeconds(long.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Exp)!))
                .UtcDateTime;

            return (await bus.InvokeAsync<Result<SessionResponse>>(new GetCurrentUserQuery(userId.Value, expiry), ct))
                .ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Current session")
        .WithDescription("Restores the session after a page load. 401 auth.unauthenticated without a valid cookie.")
        .Produces<SessionResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
    }

    private static void MapPasswordPolicy(RouteGroupBuilder group)
    {
        group.MapGet("/password-policy", () => Results.Ok(PasswordPolicyResponse.Current))
        .AllowAnonymous()
        .WithName("GetPasswordPolicy")
        .WithSummary("Password rules")
        .WithDescription("Read from the rule the server enforces, so clients need not duplicate it.")
        .Produces<PasswordPolicyResponse>();
    }
}

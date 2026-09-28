using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using MS.SS.Core.API.Middlewares;
using MS.SS.Core.API.OpenApi;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.API.Extensions;

/// <summary>
/// Everything that shapes what the client sees on the wire: how enums are serialised, how a
/// request that cannot be bound is reported, and the OpenAPI document.
/// </summary>
public static class ApiContractExtensions
{
    public static IServiceCollection AddApiContract(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

        // Outside development a body that cannot be bound (malformed JSON, a string where a number
        // is expected) is otherwise answered with a bare 400 that never reaches
        // ExceptionHandlingMiddleware. Throwing routes it through ToProblem, so it carries the
        // request.invalid code like every other failure.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        // Bodiless error responses, such as a route that does not exist, become problem+json
        // instead of an empty page.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var extensions = context.ProblemDetails.Extensions;
            if (extensions.ContainsKey("code")) return;

            extensions["code"] = CodeForStatus(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode);
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();

        services.AddOpenApi(options => options.AddSchemaTransformer<ProblemDetailsSchemaTransformer>());

        return services;
    }

    public static string CodeForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthenticated,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        StatusCodes.Status429TooManyRequests => ErrorCodes.RateLimited,
        >= StatusCodes.Status500InternalServerError => ErrorCodes.InternalError,
        _ => ErrorCodes.RequestInvalid
    };
}

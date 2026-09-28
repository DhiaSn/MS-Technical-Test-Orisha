using System.Text.Json.Serialization;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.API.Extensions;

/// <summary>
/// The single place a handler's failure becomes an HTTP status code and a stable error code.
/// </summary>
/// <remarks>
/// Every problem carries <c>extensions.code</c>, the contract the client localises from (see
/// <see cref="ErrorCodes"/>). <c>title</c> is a fallback and may be reworded at any time.
/// </remarks>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult>? onSuccess = null)
        => result.Match(
            onSuccess: value => onSuccess?.Invoke(value) ?? Results.Ok(value),
            onFailure: ToProblem,
            onNull: () => Results.NotFound());

    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null)
        => result.Match(
            onSuccess: () => onSuccess?.Invoke() ?? Results.NoContent(),
            onFailure: ToProblem);

    public static IResult ToProblem(Exception exception) => exception switch
    {
        // `errors` keeps the framework shape; `errorCodes` is index-aligned with it, field by field.
        ValidationFailedException validation => Results.ValidationProblem(
            validation.Errors.ToDictionary(kv => ToCamelCase(kv.Key), kv => kv.Value),
            title: "Invalid input.",
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = validation.Code,
                ["errorCodes"] = validation.Failures.ToDictionary(
                    kv => ToCamelCase(kv.Key),
                    kv => kv.Value.Select(e => new FieldErrorCode(e.Code, e.Parameters)).ToArray())
            }),

        KeyNotFoundException notFound => Problem(
            TitleOf(notFound, "Resource not found."), StatusCodes.Status404NotFound, CodeOf(notFound, ErrorCodes.NotFound)),

        InvalidEntityStateException invalidState => Problem(
            invalidState.Message, StatusCodes.Status400BadRequest, invalidState.Code),

        UnauthenticatedException unauthenticated => Problem(
            unauthenticated.Message, StatusCodes.Status401Unauthorized, unauthenticated.Code),

        ConflictException conflict => Problem(
            conflict.Message, StatusCodes.Status409Conflict, conflict.Code),

        ArgumentException => Problem(
            "Invalid request.", StatusCodes.Status400BadRequest, ErrorCodes.RequestInvalid),

        // The framework already chose the status for a malformed body, bad content type or oversized
        // payload; without this case it would be flattened to a 500 and look like a server fault.
        // The message is not echoed back because it can quote the offending bytes.
        BadHttpRequestException badRequest => Problem(
            "Invalid request.", badRequest.StatusCode, CodeForBadRequest(badRequest.StatusCode)),

        // Anything unrecognised is a bug. The message is withheld because it can carry internal
        // detail; the middleware logs the exception itself with its trace id.
        _ => Problem(
            "An unexpected error occurred.",
            StatusCodes.Status500InternalServerError,
            ErrorCodes.InternalError)
    };

    private static IResult Problem(string title, int statusCode, string code) =>
        Results.Problem(
            title: title,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static string CodeOf(Exception exception, string fallback) =>
        exception is ICodedException coded ? coded.Code : fallback;

    // A coded exception is one a handler raised on purpose, so its message is written for the caller.
    // A bare framework exception can come from a bug, and its message names internals.
    private static string TitleOf(Exception exception, string fallback) =>
        exception is ICodedException ? exception.Message : fallback;

    private static string CodeForBadRequest(int statusCode) => statusCode switch
    {
        StatusCodes.Status413PayloadTooLarge => ErrorCodes.RequestTooLarge,
        StatusCodes.Status415UnsupportedMediaType => ErrorCodes.UnsupportedMediaType,
        _ => ErrorCodes.RequestInvalid
    };

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

    /// <summary>One entry of <c>errorCodes.&lt;field&gt;</c>: the code plus the values its message interpolates.</summary>
    private sealed record FieldErrorCode(
        string Code,
        [property: JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyDictionary<string, string>? Params);
}

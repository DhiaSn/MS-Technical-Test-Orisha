using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.API.Extensions;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Tests.API.Extensions;

public sealed class ResultExtensionsTests
{
    private static async Task<(int Status, JsonElement Body)> Execute(IResult result)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider()
        };
        var stream = new MemoryStream();
        context.Response.Body = stream;

        await result.ExecuteAsync(context);

        stream.Position = 0;
        var body = stream.Length == 0 ? default : JsonDocument.Parse(stream).RootElement;

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_MapsTo404_WithItsCodeAndMessage()
    {
        var exception = new NotFoundException(ErrorCodes.DeliveryNotFound, "Delivery not found.");

        var (status, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.Equal(404, status);
        Assert.Equal(ErrorCodes.DeliveryNotFound, body.GetProperty("code").GetString());
        Assert.Equal("Delivery not found.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task BareKeyNotFoundException_MapsTo404_WithGenericCodeAndTitle()
    {
        var (status, body) = await Execute(ResultExtensions.ToProblem(new KeyNotFoundException("row 42 in table x")));

        Assert.Equal(404, status);
        Assert.Equal(ErrorCodes.NotFound, body.GetProperty("code").GetString());
        Assert.DoesNotContain("row 42", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ValidationFailedException_MapsTo400_WithErrorsAndIndexAlignedErrorCodes()
    {
        var exception = new ValidationFailedException(new Dictionary<string, IReadOnlyList<ValidationError>>
        {
            ["ReceivedQuantity"] =
            [
                new(ErrorCodes.QuantityOutOfRange, "Must be between 0 and 50.",
                    new Dictionary<string, string> { ["min"] = "0", ["max"] = "50" }),
                new(ErrorCodes.FieldRequired, "Required.")
            ]
        });

        var (status, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.Equal(400, status);
        Assert.Equal(ErrorCodes.ValidationFailed, body.GetProperty("code").GetString());

        var errors = body.GetProperty("errors").GetProperty("receivedQuantity");
        Assert.Equal(2, errors.GetArrayLength());

        var codes = body.GetProperty("errorCodes").GetProperty("receivedQuantity");
        Assert.Equal(ErrorCodes.QuantityOutOfRange, codes[0].GetProperty("code").GetString());
        Assert.Equal("50", codes[0].GetProperty("params").GetProperty("max").GetString());
        Assert.Equal(ErrorCodes.FieldRequired, codes[1].GetProperty("code").GetString());
        Assert.False(codes[1].TryGetProperty("params", out _));
    }

    [Fact]
    public async Task ValidationFailedException_CamelCasesFieldNames()
    {
        var exception = new ValidationFailedException("ExpectedQuantity", ErrorCodes.FieldRequired, "Required.");

        var (_, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.True(body.GetProperty("errors").TryGetProperty("expectedQuantity", out _));
        Assert.True(body.GetProperty("errorCodes").TryGetProperty("expectedQuantity", out _));
    }

    [Fact]
    public async Task InvalidEntityStateException_MapsTo400_WithItsCode()
    {
        var exception = new InvalidEntityStateException(ErrorCodes.InvalidState, "Cannot do that.");

        var (status, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.Equal(400, status);
        Assert.Equal(ErrorCodes.InvalidState, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnauthenticatedException_MapsTo401_WithItsCodeAndMessage()
    {
        var exception = new UnauthenticatedException(ErrorCodes.InvalidCredentials, "Invalid username or password.");

        var (status, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.Equal(401, status);
        Assert.Equal(ErrorCodes.InvalidCredentials, body.GetProperty("code").GetString());
        Assert.Equal("Invalid username or password.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UnauthenticatedException_WithoutACode_DefaultsToUnauthenticated()
    {
        var (status, body) = await Execute(ResultExtensions.ToProblem(new UnauthenticatedException("Sign in first.")));

        Assert.Equal(401, status);
        Assert.Equal(ErrorCodes.Unauthenticated, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConflictException_MapsTo409_WithItsCodeAndMessage()
    {
        var exception = new ConflictException(ErrorCodes.UsernameTaken, "That username is taken.");

        var (status, body) = await Execute(ResultExtensions.ToProblem(exception));

        Assert.Equal(409, status);
        Assert.Equal(ErrorCodes.UsernameTaken, body.GetProperty("code").GetString());
        Assert.Equal("That username is taken.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ConflictException_WithoutACode_DefaultsToConflict()
    {
        var (status, body) = await Execute(ResultExtensions.ToProblem(new ConflictException("Already there.")));

        Assert.Equal(409, status);
        Assert.Equal(ErrorCodes.Conflict, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ArgumentException_MapsTo400_AsRequestInvalid()
    {
        var (status, body) = await Execute(ResultExtensions.ToProblem(new ArgumentException("secret detail")));

        Assert.Equal(400, status);
        Assert.Equal(ErrorCodes.RequestInvalid, body.GetProperty("code").GetString());
        Assert.DoesNotContain("secret", body.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData(400, ErrorCodes.RequestInvalid)]
    [InlineData(413, ErrorCodes.RequestTooLarge)]
    [InlineData(415, ErrorCodes.UnsupportedMediaType)]
    public async Task BadHttpRequestException_KeepsItsStatus_AndMapsItsCode(int statusCode, string code)
    {
        var (status, body) = await Execute(
            ResultExtensions.ToProblem(new BadHttpRequestException("bytes: {\"a\":", statusCode)));

        Assert.Equal(statusCode, status);
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.DoesNotContain("bytes", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UnknownException_MapsTo500_WithServerErrorCodeAndGenericTitle()
    {
        var (status, body) = await Execute(
            ResultExtensions.ToProblem(new InvalidOperationException("connection string leaked")));

        Assert.Equal(500, status);
        Assert.Equal(ErrorCodes.InternalError, body.GetProperty("code").GetString());
        Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SuccessfulResult_WithoutOverride_ReturnsOk()
    {
        Result<string> result = "value";

        var (status, _) = await Execute(result.ToHttpResult());

        Assert.Equal(200, status);
    }

    [Fact]
    public async Task SuccessfulResult_WithOverride_UsesTheOverride()
    {
        Result<string> result = "value";

        var (status, _) = await Execute(result.ToHttpResult(_ => Results.Created("/somewhere", "value")));

        Assert.Equal(201, status);
    }

    [Fact]
    public async Task FailedResult_GoesThroughToProblem()
    {
        Result<string> result = new NotFoundException("missing");

        var (status, _) = await Execute(result.ToHttpResult());

        Assert.Equal(404, status);
    }

    [Fact]
    public async Task NullResult_ReturnsNotFound()
    {
        Result<string> result = (string?)null;

        var (status, _) = await Execute(result.ToHttpResult());

        Assert.Equal(404, status);
    }

    [Fact]
    public async Task SuccessfulVoidResult_ReturnsNoContent()
    {
        var (status, _) = await Execute(Result.Success().ToHttpResult());

        Assert.Equal(204, status);
    }

    [Fact]
    public async Task FailedVoidResult_GoesThroughToProblem()
    {
        var (status, _) = await Execute(Result.Failure(new InvalidEntityStateException("nope")).ToHttpResult());

        Assert.Equal(400, status);
    }
}

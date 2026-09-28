using System.Reflection;
using System.Text.RegularExpressions;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed partial class ErrorCodesTests
{
    [GeneratedRegex(@"^[a-z_]+(\.[a-z_]+)+$")]
    private static partial Regex CodeShape();

    private static IReadOnlyList<string> AllCodes() =>
        typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    [Fact]
    public void EveryCode_IsLowercaseDotted()
    {
        var codes = AllCodes();

        Assert.NotEmpty(codes);
        Assert.All(codes, code => Assert.Matches(CodeShape(), code));
    }

    [Fact]
    public void EveryCode_IsUnique()
    {
        var codes = AllCodes();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void EachExceptionType_DefaultsToItsGenericCode()
    {
        Assert.Equal(ErrorCodes.NotFound, new NotFoundException("x").Code);
        Assert.Equal(ErrorCodes.InvalidState, new InvalidEntityStateException("x").Code);
        Assert.Equal(ErrorCodes.InvalidState, new InvalidEntityStateException("x", new Exception()).Code);
        Assert.Equal(ErrorCodes.InvalidState, new InvalidEntityStateException().Code);
        Assert.Equal(ErrorCodes.ValidationFailed, new ValidationFailedException("f", "c", "m").Code);
        Assert.Equal(ErrorCodes.Unauthenticated, new UnauthenticatedException("x").Code);
        Assert.Equal(ErrorCodes.Conflict, new ConflictException("x").Code);
    }

    [Fact]
    public void AnExplicitCode_IsCarriedAndTheMessageKept()
    {
        var notFound = new NotFoundException(ErrorCodes.DeliveryNotFound, "message");

        Assert.Equal(ErrorCodes.DeliveryNotFound, notFound.Code);
        Assert.Equal("message", notFound.Message);
    }

    [Fact]
    public void NotFoundException_KeepsItsBclBaseType_SoExistingCatchesStillMatch()
    {
        Assert.IsAssignableFrom<KeyNotFoundException>(new NotFoundException("x"));
    }

    [Fact]
    public void ValidationFailedException_ExposesFallbackMessagesPerField()
    {
        var exception = new ValidationFailedException("quantity", ErrorCodes.QuantityOutOfRange, "Out of range.");

        Assert.Equal(["Out of range."], exception.Errors["quantity"]);
        Assert.Equal(ErrorCodes.QuantityOutOfRange, exception.Failures["quantity"][0].Code);
    }
}

using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Guards;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed class EnsureTests
{
    [Fact]
    public void NoRuleBroken_HasNoErrors_AndNoException()
    {
        var ensure = Ensure.That().NotNullOrWhiteSpace("username", "magasinier", "Required.");

        Assert.False(ensure.HasErrors);
        Assert.Null(ensure.AsException());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_BlankValue_ReportsRequired(string? value)
    {
        var exception = Ensure.That().NotNullOrWhiteSpace("username", value, "Required.").AsException();

        Assert.NotNull(exception);
        Assert.Equal(ErrorCodes.FieldRequired, exception.Failures["username"][0].Code);
        Assert.Equal(["Required."], exception.Errors["username"]);
    }

    [Fact]
    public void NotNullOrWhiteSpace_AcceptsAnExplicitCode()
    {
        var exception = Ensure.That()
            .NotNullOrWhiteSpace("password", "", "Required.", ErrorCodes.PasswordRequired)
            .AsException();

        Assert.Equal(ErrorCodes.PasswordRequired, exception!.Failures["password"][0].Code);
    }

    [Fact]
    public void MaxLength_TooLong_ReportsItsBound()
    {
        var exception = Ensure.That().MaxLength("displayName", new string('a', 6), 5, "Too long.").AsException();

        var error = Assert.Single(exception!.Failures["displayName"]);
        Assert.Equal(ErrorCodes.FieldTooLong, error.Code);
        Assert.Equal("5", error.Parameters!["max"]);
    }

    [Theory]
    [InlineData("abcde")]
    [InlineData("")]
    [InlineData(null)]
    public void MaxLength_WithinTheLimit_OrMissing_ReportsNothing(string? value)
    {
        Assert.False(Ensure.That().MaxLength("displayName", value, 5, "Too long.").HasErrors);
    }

    [Fact]
    public void Fail_RecordsAnAlreadyBuiltError_OnTheGivenField()
    {
        var error = new ValidationError(ErrorCodes.PasswordTooShort, "Too short.");

        var exception = Ensure.That().Fail("password", error).AsException();

        Assert.Same(error, Assert.Single(exception!.Failures["password"]));
    }

    [Fact]
    public void Failures_OnTheSameField_Accumulate_InOrder()
    {
        var exception = Ensure.That()
            .Fail("password", new ValidationError(ErrorCodes.PasswordTooShort, "Too short."))
            .Fail("password", new ValidationError(ErrorCodes.PasswordMissingDigit, "No digit."))
            .AsException();

        Assert.Equal(
            [ErrorCodes.PasswordTooShort, ErrorCodes.PasswordMissingDigit],
            exception!.Failures["password"].Select(e => e.Code));
    }

    [Fact]
    public void Failures_OnDifferentFields_AreReportedTogether()
    {
        var exception = Ensure.That()
            .NotNullOrWhiteSpace("username", "", "Required.")
            .MaxLength("displayName", new string('a', 10), 5, "Too long.")
            .AsException();

        Assert.Equal(["displayName", "username"], exception!.Failures.Keys.Order());
    }

    [Fact]
    public void ThrowIfInvalid_WithoutErrors_DoesNotThrow()
    {
        Assert.Null(Record.Exception(Ensure.That().NotNullOrWhiteSpace("username", "x", "Required.").ThrowIfInvalid));
    }

    [Fact]
    public void ThrowIfInvalid_WithErrors_ThrowsValidationFailed()
    {
        var ensure = Ensure.That().NotNullOrWhiteSpace("username", "", "Required.");

        Assert.Throws<ValidationFailedException>(ensure.ThrowIfInvalid);
    }
}

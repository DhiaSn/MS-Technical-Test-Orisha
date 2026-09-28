using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed class PasswordValidatorTests
{
    private static string[] CodesOf(string? password) =>
        PasswordValidator.Validate(password).Errors.Select(e => e.Code).ToArray();

    [Fact]
    public void Validate_MeetsEveryRule_IsValid()
    {
        var result = PasswordValidator.Validate("Magasin2026");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ExactlyTheMinimumLength_IsValid()
    {
        Assert.True(PasswordValidator.Validate("Abcdef12").IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankPassword_ReportsOnlyRequired(string? password)
    {
        Assert.Equal([ErrorCodes.PasswordRequired], CodesOf(password));
    }

    [Fact]
    public void Validate_TooShort_ReportsTheLengthAndItsBound()
    {
        var error = Assert.Single(PasswordValidator.Validate("Abcde12").Errors);

        Assert.Equal(ErrorCodes.PasswordTooShort, error.Code);
        Assert.Equal("8", error.Parameters!["min"]);
    }

    [Fact]
    public void Validate_NoUppercase_ReportsMissingUppercase()
    {
        Assert.Equal([ErrorCodes.PasswordMissingUppercase], CodesOf("magasin2026"));
    }

    [Fact]
    public void Validate_NoLowercase_ReportsMissingLowercase()
    {
        Assert.Equal([ErrorCodes.PasswordMissingLowercase], CodesOf("MAGASIN2026"));
    }

    [Fact]
    public void Validate_NoDigit_ReportsMissingDigit()
    {
        Assert.Equal([ErrorCodes.PasswordMissingDigit], CodesOf("MagasinierSansChiffre"));
    }

    [Fact]
    public void Validate_EveryRuleBroken_ReportsAllFour()
    {
        Assert.Equal(
            [
                ErrorCodes.PasswordTooShort,
                ErrorCodes.PasswordMissingUppercase,
                ErrorCodes.PasswordMissingLowercase,
                ErrorCodes.PasswordMissingDigit
            ],
            CodesOf("-----"));
    }

    [Fact]
    public void Validate_SpecialCharacterNotRequired()
    {
        Assert.True(PasswordValidator.Validate("Magasin2026").IsValid);
    }
}

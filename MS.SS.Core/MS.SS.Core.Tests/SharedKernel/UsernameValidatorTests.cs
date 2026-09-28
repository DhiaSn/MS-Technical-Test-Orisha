using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed class UsernameValidatorTests
{
    [Theory]
    [InlineData("magasinier")]
    [InlineData("a.b-c_9")]
    [InlineData("a")]
    public void IsValid_AllowedCharacters_ReturnsTrue(string username)
    {
        Assert.True(UsernameValidator.IsValid(username));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("Upper")]
    [InlineData("a@b")]
    [InlineData("éric")]
    public void IsValid_DisallowedInput_ReturnsFalse(string? username)
    {
        Assert.False(UsernameValidator.IsValid(username));
    }

    [Fact]
    public void IsValid_AtTheLimit_ReturnsTrue()
    {
        Assert.True(UsernameValidator.IsValid(new string('a', UsernameValidator.MaxLength)));
    }

    [Fact]
    public void IsValid_LongerThanTheLimit_ReturnsFalse()
    {
        Assert.False(UsernameValidator.IsValid(new string('a', 51)));
    }

    [Fact]
    public void Normalize_TrimsAndLowercases()
    {
        Assert.Equal("magasinier", UsernameValidator.Normalize("  Magasinier "));
    }

    [Theory]
    [InlineData("Magasinier")]
    [InlineData("A.B-C_9")]
    public void Normalize_MakesAMixedCaseNameValid(string username)
    {
        Assert.True(UsernameValidator.IsValid(UsernameValidator.Normalize(username)));
    }
}

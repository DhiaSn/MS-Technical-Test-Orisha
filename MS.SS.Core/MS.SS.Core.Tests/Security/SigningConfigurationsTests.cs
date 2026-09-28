using MS.SS.Core.Security.Models;

namespace MS.SS.Core.Tests.Security;

public class SigningConfigurationsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too-short")]
    public void A_missing_or_short_secret_is_refused_at_construction(string secret)
    {
        Assert.Throws<InvalidOperationException>(() => new SigningConfigurations(secret));
    }

    [Fact]
    public void The_refusal_names_the_setting_to_fix()
    {
        var missing = Assert.Throws<InvalidOperationException>(() => new SigningConfigurations(""));
        var tooShort = Assert.Throws<InvalidOperationException>(() => new SigningConfigurations("too-short"));

        Assert.Contains("TokenOptions:Secret", missing.Message);
        Assert.Contains("TokenOptions:Secret", tooShort.Message);
    }

    [Fact]
    public void The_refusal_never_echoes_the_secret()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new SigningConfigurations("too-short"));

        Assert.DoesNotContain("too-short", error.Message);
    }

    [Fact]
    public void A_long_enough_secret_is_accepted()
    {
        Assert.NotNull(new SigningConfigurations(new string('k', 32)).SigningCredentials);
    }

    [Fact]
    public void The_secret_length_is_measured_in_bytes_not_characters()
    {
        Assert.Throws<InvalidOperationException>(() => new SigningConfigurations(new string('k', 31)));
        Assert.NotNull(new SigningConfigurations(new string('é', 16)).SigningCredentials);
    }
}

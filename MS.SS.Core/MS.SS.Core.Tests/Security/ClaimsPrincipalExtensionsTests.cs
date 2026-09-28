using System.Security.Claims;
using MS.SS.Core.Security.Extensions;

namespace MS.SS.Core.Tests.Security;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void The_user_id_is_read_from_the_name_identifier_claim()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, PrincipalWith(id.ToString()).GetUserId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void A_missing_or_invalid_user_id_reads_as_nobody(string? value)
    {
        Assert.Null(PrincipalWith(value).GetUserId());
    }

    private static ClaimsPrincipal PrincipalWith(string? userId)
    {
        var claims = userId is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}

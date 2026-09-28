using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Tests.Modules.Identity.Domain;

public class UserTests
{
    [Fact]
    public void Registering_creates_an_active_user_who_has_never_signed_in()
    {
        var user = User.Register("magasinier", "Magasinier", "hash", IdentityRoles.Operator);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.True(user.IsActive);
        Assert.Null(user.LastLoginAt);
    }

    [Theory]
    [InlineData("", "Name", "hash", "Operator")]
    [InlineData("user", " ", "hash", "Operator")]
    [InlineData("user", "Name", "", "Operator")]
    [InlineData("user", "Name", "hash", "")]
    public void Every_field_is_required(string username, string displayName, string hash, string role)
    {
        Assert.Throws<InvalidEntityStateException>(() => User.Register(username, displayName, hash, role));
    }

    [Fact]
    public void Recording_a_login_and_deactivating_are_visible()
    {
        var user = User.Register("magasinier", "Magasinier", "hash", IdentityRoles.Operator);
        var at = new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);

        user.RecordLogin(at);
        user.Deactivate();

        Assert.Equal(at, user.LastLoginAt);
        Assert.False(user.IsActive);
    }

    [Fact]
    public void The_username_is_stored_normalised()
    {
        var user = User.Register(" Magasinier ", "Magasinier", "hash", IdentityRoles.Operator);

        Assert.Equal("magasinier", user.Username);
    }

    [Theory]
    [InlineData("not valid!")]
    [InlineData("a@b.c")]
    [InlineData("café")]
    public void An_invalid_username_is_refused_with_its_own_code(string username)
    {
        var error = Assert.Throws<InvalidEntityStateException>(
            () => User.Register(username, "Name", "hash", IdentityRoles.Operator));

        Assert.Equal(ErrorCodes.UsernameInvalid, error.Code);
    }

    [Fact]
    public void A_username_longer_than_the_column_is_refused()
    {
        var error = Assert.Throws<InvalidEntityStateException>(
            () => User.Register(new string('a', 51), "Name", "hash", IdentityRoles.Operator));

        Assert.Equal(ErrorCodes.UsernameInvalid, error.Code);
    }

    [Fact]
    public void A_display_name_longer_than_the_column_is_refused()
    {
        var error = Assert.Throws<InvalidEntityStateException>(
            () => User.Register("magasinier", new string('x', 101), "hash", IdentityRoles.Operator));

        Assert.Equal(ErrorCodes.FieldTooLong, error.Code);
    }

    [Fact]
    public void Upgrading_the_password_hash_replaces_it_and_rejects_an_empty_one()
    {
        var user = User.Register("magasinier", "Magasinier", "old-hash", IdentityRoles.Operator);

        user.UpgradePasswordHash("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Throws<InvalidEntityStateException>(() => user.UpgradePasswordHash(" "));
    }
}

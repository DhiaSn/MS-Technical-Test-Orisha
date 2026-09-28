using MS.SS.Core.Modules.Identity.Application.Commands.AuthenticationCommands;
using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Models;
using MS.SS.Core.Security.Models.Tokens;

namespace MS.SS.Core.Tests.Modules.Identity.Application;

public class SecretsInToStringTests
{
    private const string Password = "Sup3r-Secret-Passw0rd";
    private const string Token = "eyJ.header.payload.signature";

    private static readonly SignInRequest SignIn = new("magasinier", Password);
    private static readonly RegisterRequest Register = new("magasinier", "Magasinier", Password);
    private static readonly AccessToken AccessToken = new(Token, new DateTime(2026, 9, 28, 16, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void A_sign_in_request_does_not_print_the_password()
    {
        var text = SignIn.ToString();

        Assert.DoesNotContain(Password, text);
        Assert.Contains("magasinier", text);
    }

    [Fact]
    public void A_register_request_does_not_print_the_password()
    {
        var text = Register.ToString();

        Assert.DoesNotContain(Password, text);
        Assert.Contains("magasinier", text);
        Assert.Contains("Magasinier", text);
    }

    [Fact]
    public void The_commands_wrapping_the_requests_do_not_print_the_password()
    {
        Assert.DoesNotContain(Password, new SignInCommand(SignIn).ToString());
        Assert.DoesNotContain(Password, new RegisterOperatorCommand(Register).ToString());
    }

    [Fact]
    public void An_access_token_does_not_print_its_value()
    {
        var text = AccessToken.ToString();

        Assert.DoesNotContain(Token, text);
        Assert.Contains("2026", text);
    }

    [Fact]
    public void An_authentication_result_does_not_print_the_token()
    {
        var response = new SessionResponse(Guid.NewGuid(), "magasinier", "Magasinier", "Operator", AccessToken.ExpiresAt);

        Assert.DoesNotContain(Token, new AuthenticationResult(response, AccessToken).ToString());
    }
}

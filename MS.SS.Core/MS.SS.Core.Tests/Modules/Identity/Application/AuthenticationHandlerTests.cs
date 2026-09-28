using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MS.SS.Core.Modules.Identity.Application.Commands.AuthenticationCommands;
using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Application.Queries.UserQueries;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Validators;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Npgsql;

namespace MS.SS.Core.Tests.Modules.Identity.Application;

public class SignInHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly User _user = User.Register("magasinier", "Magasinier", "stored-hash", IdentityRoles.Operator);

    private SignInHandler Handler => new(_users, _unitOfWork, _passwords, _tokens, TimeProvider.System, NullLogger<SignInHandler>.Instance);

    public SignInHandlerTests()
    {
        _users.FindByUsernameAsync("magasinier", Arg.Any<CancellationToken>()).Returns(_user);
        _passwords.VerifyPassword("Reception2026", "stored-hash").Returns(true);
        _tokens.CreateAccessToken(Arg.Any<TokenPrincipal>()).Returns(new AccessToken("jwt", DateTime.UtcNow.AddHours(8)));
    }

    [Fact]
    public async Task Valid_credentials_open_a_session_and_record_the_login()
    {
        var result = await Handler.Handle(new(new("Magasinier ", "Reception2026")), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("magasinier", result.Value.Response.Username);
        Assert.Equal("jwt", result.Value.AccessToken.Token);
        Assert.NotNull(_user.LastLoginAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_response_never_contains_the_token_or_the_hash()
    {
        var result = await Handler.Handle(new(new("magasinier", "Reception2026")), default);

        var json = JsonSerializer.Serialize(result.Value.Response);
        Assert.DoesNotContain("jwt", json);
        Assert.DoesNotContain("stored-hash", json);
    }

    [Fact]
    public async Task A_wrong_password_a_missing_account_and_an_inactive_account_are_refused_identically()
    {
        _passwords.VerifyPassword("wrong", "stored-hash").Returns(false);
        var wrongPassword = await Handler.Handle(new(new("magasinier", "wrong")), default);

        var missing = await Handler.Handle(new(new("nobody", "Reception2026")), default);

        _user.Deactivate();
        _passwords.ClearReceivedCalls();
        var inactive = await Handler.Handle(new(new("magasinier", "Reception2026")), default);

        foreach (var refusal in new[] { wrongPassword, missing, inactive })
        {
            var error = Assert.IsType<UnauthenticatedException>(refusal.Exception);
            Assert.Equal(ErrorCodes.InvalidCredentials, error.Code);
            Assert.Equal("Incorrect username or password.", error.Message);
        }

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        _tokens.DidNotReceiveWithAnyArgs().CreateAccessToken(default!);
        _passwords.Received(1).VerifyPassword("Reception2026", "stored-hash");
    }

    [Fact]
    public async Task A_missing_account_still_spends_the_hashing_time()
    {
        await Handler.Handle(new(new("nobody", "Reception2026")), default);

        _passwords.Received(1).SpendVerificationTime("Reception2026");
    }

    [Theory]
    [InlineData(null, "Reception2026")]
    [InlineData("", "Reception2026")]
    [InlineData("magasinier", "")]
    [InlineData("a@b.c", "Reception2026")]
    [InlineData("has space", "Reception2026")]
    public async Task Malformed_input_is_the_same_generic_refusal_and_never_reaches_the_repository(string? username, string password)
    {
        var result = await Handler.Handle(new(new(username, password)), default);

        var error = Assert.IsType<UnauthenticatedException>(result.Exception);
        Assert.Equal(ErrorCodes.InvalidCredentials, error.Code);
        Assert.Equal("Incorrect username or password.", error.Message);
        await _users.DidNotReceiveWithAnyArgs().FindByUsernameAsync(default!, default);
    }

    [Fact]
    public async Task An_old_hash_is_upgraded_on_a_successful_sign_in()
    {
        _passwords.NeedsRehash("stored-hash").Returns(true);
        _passwords.HashPassword("Reception2026").Returns("new-hash");

        await Handler.Handle(new(new("magasinier", "Reception2026")), default);

        Assert.Equal("new-hash", _user.PasswordHash);
    }

    [Fact]
    public async Task A_current_hash_is_left_alone()
    {
        await Handler.Handle(new(new("magasinier", "Reception2026")), default);

        Assert.Equal("stored-hash", _user.PasswordHash);
        _passwords.DidNotReceiveWithAnyArgs().HashPassword(default!);
    }
}

public class RegisterOperatorHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();

    private RegisterOperatorHandler Handler => new(_users, _unitOfWork, _passwords, _tokens, TimeProvider.System, NullLogger<RegisterOperatorHandler>.Instance);

    public RegisterOperatorHandlerTests()
    {
        _passwords.ValidateStrength(Arg.Any<string?>()).Returns(call => PasswordValidator.Validate(call.Arg<string?>()));
        _passwords.HashPassword(Arg.Any<string>()).Returns("new-hash");
        _tokens.CreateAccessToken(Arg.Any<TokenPrincipal>()).Returns(new AccessToken("jwt", DateTime.UtcNow.AddHours(8)));
    }

    [Fact]
    public async Task A_valid_registration_stores_a_hashed_operator_and_opens_a_session()
    {
        var result = await Handler.Handle(new(new("Nouveau.Op", "Nouvel Opérateur", "Reception2026")), default);

        Assert.True(result.IsSuccess);
        _users.Received(1).Add(Arg.Is<User>(u =>
            u.Username == "nouveau.op" && u.PasswordHash == "new-hash" && u.Role == IdentityRoles.Operator));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Registering_opens_a_session_so_the_first_login_is_recorded()
    {
        User? added = null;
        _users.When(u => u.Add(Arg.Any<User>())).Do(call => added = call.Arg<User>());

        await Handler.Handle(new(new("nouveau.op", "Nouvel Opérateur", "Reception2026")), default);

        Assert.NotNull(added!.LastLoginAt);
    }

    [Fact]
    public async Task The_session_carries_the_new_operator_and_the_token_expiry_but_not_the_token()
    {
        var result = await Handler.Handle(new(new("nouveau.op", "Nouvel Opérateur", "Reception2026")), default);

        var response = result.Value.Response;
        Assert.Equal("nouveau.op", response.Username);
        Assert.Equal("Nouvel Opérateur", response.DisplayName);
        Assert.Equal(IdentityRoles.Operator, response.Role);
        Assert.Equal("jwt", result.Value.AccessToken.Token);
        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("jwt", json);
        Assert.DoesNotContain("new-hash", json);
    }

    [Fact]
    public async Task Every_broken_rule_is_reported_on_its_own_field_and_nothing_is_saved()
    {
        var result = await Handler.Handle(new(new("", "", "weak")), default);

        var failure = Assert.IsType<ValidationFailedException>(result.Exception);
        Assert.Equal(ErrorCodes.FieldRequired, failure.Failures["username"][0].Code);
        Assert.Equal(ErrorCodes.FieldRequired, failure.Failures["displayName"][0].Code);
        Assert.Contains(failure.Failures["password"], e => e.Code == ErrorCodes.PasswordTooShort);
        Assert.Contains(failure.Failures["password"], e => e.Code == ErrorCodes.PasswordMissingUppercase);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task An_invalid_username_is_reported_with_its_own_code()
    {
        var result = await Handler.Handle(new(new("not valid!", "Name", "Reception2026")), default);

        Assert.Equal(ErrorCodes.UsernameInvalid,
            Assert.IsType<ValidationFailedException>(result.Exception).Failures["username"][0].Code);
    }

    [Fact]
    public async Task A_display_name_over_the_column_length_is_rejected()
    {
        var result = await Handler.Handle(new(new("magasinier", new string('x', 101), "Reception2026")), default);

        Assert.Equal(ErrorCodes.FieldTooLong,
            Assert.IsType<ValidationFailedException>(result.Exception).Failures["displayName"][0].Code);
    }

    [Fact]
    public async Task A_taken_username_is_a_conflict()
    {
        _users.UsernameExistsAsync("magasinier", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler.Handle(new(new("magasinier", "Name", "Reception2026")), default);

        Assert.Equal(ErrorCodes.UsernameTaken, Assert.IsType<ConflictException>(result.Exception).Code);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Losing_the_unique_index_race_is_the_same_conflict()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateException("dup",
            new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
                constraintName: "ux_users_username")));

        var result = await Handler.Handle(new(new("magasinier", "Name", "Reception2026")), default);

        Assert.Equal(ErrorCodes.UsernameTaken, Assert.IsType<ConflictException>(result.Exception).Code);
        _tokens.DidNotReceiveWithAnyArgs().CreateAccessToken(default!);
    }

    [Fact]
    public async Task A_unique_violation_on_another_constraint_is_not_disguised_as_a_conflict()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateException("dup",
            new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
                constraintName: "pk_users")));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handler.Handle(new(new("magasinier", "Name", "Reception2026")), default));
    }

    [Fact]
    public async Task Any_other_persistence_failure_is_not_disguised_as_a_conflict()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateException("boom",
            new PostgresException("other", "ERROR", "ERROR", PostgresErrorCodes.NotNullViolation)));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handler.Handle(new(new("magasinier", "Name", "Reception2026")), default));
    }
}

public class GetCurrentUserQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly DateTime _expiry = new(2026, 9, 28, 16, 0, 0, DateTimeKind.Utc);

    private GetCurrentUserQueryHandler Handler => new(_users);

    [Fact]
    public async Task An_active_user_gets_a_session_with_the_expiry_of_the_presented_token()
    {
        var user = User.Register("magasinier", "Magasinier", "hash", IdentityRoles.Operator);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await Handler.Handle(new(user.Id, _expiry), default);

        Assert.Equal(new SessionResponse(user.Id, "magasinier", "Magasinier", IdentityRoles.Operator, _expiry), result.Value);
    }

    [Fact]
    public async Task An_unknown_user_id_is_unauthenticated()
    {
        var result = await Handler.Handle(new(Guid.NewGuid(), _expiry), default);

        Assert.IsType<UnauthenticatedException>(result.Exception);
    }

    [Fact]
    public async Task An_inactive_user_is_unauthenticated()
    {
        var user = User.Register("magasinier", "Magasinier", "hash", IdentityRoles.Operator);
        user.Deactivate();
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await Handler.Handle(new(user.Id, _expiry), default);

        Assert.IsType<UnauthenticatedException>(result.Exception);
    }
}

public class PasswordPolicyResponseTests
{
    [Fact]
    public void The_published_policy_mirrors_the_enforced_one()
    {
        var policy = PasswordPolicyResponse.Current;

        Assert.Equal(PasswordPolicy.MinimumLength, policy.MinimumLength);
        Assert.Equal(PasswordPolicy.RequireUppercase, policy.RequireUppercase);
        Assert.Equal(PasswordPolicy.RequireLowercase, policy.RequireLowercase);
        Assert.Equal(PasswordPolicy.RequireDigit, policy.RequireDigit);
    }
}

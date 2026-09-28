using Microsoft.Extensions.Logging;
using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Application.Models;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;
using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Modules.Identity.Application.Commands.AuthenticationCommands;

public sealed record SignInCommand(SignInRequest Request);

public sealed class SignInHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordService passwords,
    ITokenService tokens,
    TimeProvider clock,
    ILogger<SignInHandler> logger)
{
    // One message for every failure path, so no branch can be more specific than another.
    private const string GenericFailure = "Incorrect username or password.";

    public async Task<Result<AuthenticationResult>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var username = UsernameValidator.Normalize(command.Request.Username ?? string.Empty);
        var password = command.Request.Password ?? string.Empty;

        if (!UsernameValidator.IsValid(username) || password.Length == 0) return Refuse("malformed credentials");

        var user = await users.FindByUsernameAsync(username, cancellationToken);

        // Same hashing cost when the account does not exist, so response time does not reveal it.
        if (user is null)
        {
            passwords.SpendVerificationTime(password);
            return Refuse("unknown account");
        }

        if (!passwords.VerifyPassword(password, user.PasswordHash)) return Refuse("wrong password", user.Id);
        if (!user.IsActive) return Refuse("inactive account", user.Id);

        if (passwords.NeedsRehash(user.PasswordHash)) user.UpgradePasswordHash(passwords.HashPassword(password));
        user.RecordLogin(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = tokens.CreateAccessToken(new TokenPrincipal(user.Id, user.Role));
        logger.LogInformation("Sign-in succeeded for user {UserId}", user.Id);

        return new AuthenticationResult(SessionResponse.FromUser(user, token.ExpiresAt), token);
    }

    // The username is deliberately not logged: a mistyped password often lands in that field.
    private Result<AuthenticationResult> Refuse(string reason, Guid? userId = null)
    {
        logger.LogInformation("Sign-in refused for user {UserId}: {Reason}", userId, reason);
        return new UnauthenticatedException(ErrorCodes.InvalidCredentials, GenericFailure);
    }
}

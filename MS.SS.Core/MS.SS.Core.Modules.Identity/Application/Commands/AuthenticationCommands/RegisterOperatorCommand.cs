using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Application.Models;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Security.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Guards;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;
using MS.SS.Core.SharedKernel.Validators;
using Npgsql;

namespace MS.SS.Core.Modules.Identity.Application.Commands.AuthenticationCommands;

public sealed record RegisterOperatorCommand(RegisterRequest Request);

public sealed class RegisterOperatorHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordService passwords,
    ITokenService tokens,
    TimeProvider clock,
    ILogger<RegisterOperatorHandler> logger)
{
    public async Task<Result<AuthenticationResult>> Handle(RegisterOperatorCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var username = UsernameValidator.Normalize(request.Username ?? string.Empty);
        var displayName = request.DisplayName?.Trim() ?? string.Empty;

        var check = Ensure.That();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            check.Fail("username", new ValidationError(ErrorCodes.FieldRequired, "The username is required."));
        }
        else if (!UsernameValidator.IsValid(username))
        {
            check.Fail("username", new ValidationError(ErrorCodes.UsernameInvalid,
                "The username may only contain lowercase letters, digits, '.', '_' and '-'."));
        }

        check.NotNullOrWhiteSpace("displayName", request.DisplayName, "The display name is required.")
             .MaxLength("displayName", displayName, User.DisplayNameMaxLength, "The display name is too long.");

        foreach (var error in passwords.ValidateStrength(request.Password).Errors) check.Fail("password", error);

        if (check.AsException() is { } invalid) return invalid;

        if (await users.UsernameExistsAsync(username, cancellationToken)) return UsernameTaken();

        var user = User.Register(username, displayName, passwords.HashPassword(request.Password!), IdentityRoles.Operator);
        user.RecordLogin(clock.GetUtcNow().UtcDateTime);
        users.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IUserRepository.UniqueUsernameIndex
        })
        {
            // Two identical registrations passed the existence check together; the unique index decided.
            return UsernameTaken();
        }

        var token = tokens.CreateAccessToken(new TokenPrincipal(user.Id, user.Role));
        logger.LogInformation("Operator {UserId} registered", user.Id);

        return new AuthenticationResult(SessionResponse.FromUser(user, token.ExpiresAt), token);
    }

    private static ConflictException UsernameTaken() =>
        new(ErrorCodes.UsernameTaken, "This username is already in use.");
}

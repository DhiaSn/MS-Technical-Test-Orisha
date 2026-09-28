using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Models;
using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Modules.Identity.Domain.Entities;

public sealed class User : AuditableEntity
{
    public const int DisplayNameMaxLength = 100;

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    public static User Register(string username, string displayName, string passwordHash, string role) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = ValidUsername(username),
            DisplayName = ValidDisplayName(displayName),
            PasswordHash = Required(passwordHash, nameof(passwordHash)),
            Role = Required(role, nameof(role))
        };

    public void RecordLogin(DateTime at) => LastLoginAt = at;

    public void UpgradePasswordHash(string passwordHash) =>
        PasswordHash = Required(passwordHash, nameof(passwordHash));

    public void Deactivate() => IsActive = false;

    private static string ValidUsername(string? username)
    {
        var normalized = UsernameValidator.Normalize(Required(username, nameof(username)));

        return UsernameValidator.IsValid(normalized)
            ? normalized
            : throw new InvalidEntityStateException(ErrorCodes.UsernameInvalid, "username is not a valid username.");
    }

    private static string ValidDisplayName(string? displayName)
    {
        var trimmed = Required(displayName, nameof(displayName));

        return trimmed.Length <= DisplayNameMaxLength
            ? trimmed
            : throw new InvalidEntityStateException(ErrorCodes.FieldTooLong, "displayName is too long.");
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidEntityStateException(ErrorCodes.InvalidState, $"{field} is required.")
            : value.Trim();
}

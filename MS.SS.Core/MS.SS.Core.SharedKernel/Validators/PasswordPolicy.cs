namespace MS.SS.Core.SharedKernel.Validators;

/// <summary>
/// The password rules, as data. <see cref="PasswordValidator"/> enforces them and the
/// <c>password-policy</c> endpoint publishes them, so the SPA never carries its own copy.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const bool RequireUppercase = true;
    public const bool RequireLowercase = true;
    public const bool RequireDigit = true;
    public const bool RequireSpecialCharacter = false;
}

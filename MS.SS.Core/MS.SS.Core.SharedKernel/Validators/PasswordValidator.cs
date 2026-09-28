using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.SharedKernel.Validators;

/// <summary>Enforces <see cref="PasswordPolicy"/>, reporting every rule that fails.</summary>
public static class PasswordValidator
{
    public static PasswordValidationResult Validate(string? password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add(new ValidationError(
                ErrorCodes.PasswordRequired, "The password is required."));
            return result;
        }

        if (password.Length < PasswordPolicy.MinimumLength)
        {
            result.Errors.Add(new ValidationError(
                ErrorCodes.PasswordTooShort,
                $"The password must contain at least {PasswordPolicy.MinimumLength} characters.",
                new Dictionary<string, string> { ["min"] = PasswordPolicy.MinimumLength.ToString() }));
        }

        if (PasswordPolicy.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.Errors.Add(new ValidationError(
                ErrorCodes.PasswordMissingUppercase, "The password must contain at least one uppercase letter."));
        }

        if (PasswordPolicy.RequireLowercase && !password.Any(char.IsLower))
        {
            result.Errors.Add(new ValidationError(
                ErrorCodes.PasswordMissingLowercase, "The password must contain at least one lowercase letter."));
        }

        if (PasswordPolicy.RequireDigit && !password.Any(char.IsDigit))
        {
            result.Errors.Add(new ValidationError(
                ErrorCodes.PasswordMissingDigit, "The password must contain at least one digit."));
        }

        return result;
    }
}

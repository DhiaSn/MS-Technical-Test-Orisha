using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Modules.Identity.Application.Dtos;

public sealed record PasswordPolicyResponse(
    int MinimumLength, bool RequireUppercase, bool RequireLowercase, bool RequireDigit)
{
    public static PasswordPolicyResponse Current => new(
        PasswordPolicy.MinimumLength,
        PasswordPolicy.RequireUppercase,
        PasswordPolicy.RequireLowercase,
        PasswordPolicy.RequireDigit);
}

using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Security.Interfaces;

public interface IPasswordService
{
    /// <summary>Hashes a plaintext password. The result embeds its own salt and iteration count.</summary>
    string HashPassword(string password);

    /// <summary>Constant-time verification of a plaintext password against a stored hash.</summary>
    bool VerifyPassword(string providedPassword, string? passwordHash);

    /// <summary>
    /// Costs as much as <see cref="VerifyPassword"/> against a real hash. Called when the account does not exist,
    /// so a missing account and a wrong password take the same time and the difference cannot be measured.
    /// </summary>
    void SpendVerificationTime(string providedPassword);

    /// <summary>
    /// True when the stored hash was produced with an older work factor and should be upgraded on
    /// the next successful sign-in.
    /// </summary>
    bool NeedsRehash(string passwordHash);

    PasswordValidationResult ValidateStrength(string? password);
}

using System.Security.Cryptography;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.SharedKernel.Validators;

namespace MS.SS.Core.Security.Services;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing.
/// </summary>
/// <remarks>
/// Stored format (Base64 of): [version:1][iterations:4 big-endian][salt:16][subkey:32] = 53 bytes.
///
/// The iteration count lives inside the hash rather than in a constant, so raising
/// <see cref="SecurityConstants.PasswordIterations"/> does not invalidate existing passwords:
/// old hashes still verify at their original cost, and <see cref="NeedsRehash"/> flags them for a
/// transparent upgrade on the user's next successful sign-in.
/// </remarks>
public sealed class PasswordService : IPasswordService
{
    private const byte FormatVersion = 1;
    private const int SaltSize = 16;
    private const int SubkeySize = 32;
    private const int HeaderSize = 1 + 4;
    private const int TotalSize = HeaderSize + SaltSize + SubkeySize;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var iterations = SecurityConstants.PasswordIterations;
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, SubkeySize);

        var output = new byte[TotalSize];
        output[0] = FormatVersion;
        WriteInt32BigEndian(output.AsSpan(1, 4), iterations);
        salt.CopyTo(output.AsSpan(HeaderSize, SaltSize));
        subkey.CopyTo(output.AsSpan(HeaderSize + SaltSize, SubkeySize));

        return Convert.ToBase64String(output);
    }

    public bool VerifyPassword(string providedPassword, string? passwordHash)
    {
        if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(passwordHash))
            return false;

        if (!TryDecode(passwordHash, out var iterations, out var salt, out var expectedSubkey))
            return false;

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword, salt, iterations, HashAlgorithmName.SHA256, SubkeySize);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    public void SpendVerificationTime(string providedPassword) => VerifyPassword(providedPassword, TimingEqualizerHash.Value);

    public bool NeedsRehash(string passwordHash)
    {
        if (!TryDecode(passwordHash, out var iterations, out _, out _))
            return true;

        return iterations < SecurityConstants.PasswordIterations;
    }

    public PasswordValidationResult ValidateStrength(string? password) => PasswordValidator.Validate(password);

    private static bool TryDecode(string passwordHash, out int iterations, out byte[] salt, out byte[] subkey)
    {
        iterations = 0;
        salt = [];
        subkey = [];

        Span<byte> buffer = stackalloc byte[TotalSize];
        if (!Convert.TryFromBase64String(passwordHash, buffer, out var written) || written != TotalSize)
            return false;

        if (buffer[0] != FormatVersion)
            return false;

        iterations = ReadInt32BigEndian(buffer.Slice(1, 4));
        if (iterations <= 0)
            return false;

        salt = buffer.Slice(HeaderSize, SaltSize).ToArray();
        subkey = buffer.Slice(HeaderSize + SaltSize, SubkeySize).ToArray();
        return true;
    }

    private static void WriteInt32BigEndian(Span<byte> destination, int value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    private static int ReadInt32BigEndian(ReadOnlySpan<byte> source) =>
        (source[0] << 24) | (source[1] << 16) | (source[2] << 8) | source[3];
}

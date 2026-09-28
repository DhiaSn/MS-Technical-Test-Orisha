using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using MS.SS.Core.Security.Services;

namespace MS.SS.Core.Tests.Security;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void A_hash_verifies_only_the_password_it_came_from()
    {
        var hash = _service.HashPassword("Reception2026");

        Assert.True(_service.VerifyPassword("Reception2026", hash));
        Assert.False(_service.VerifyPassword("reception2026", hash));
        Assert.DoesNotContain("Reception2026", hash);
    }

    [Fact]
    public void Hashing_the_same_password_twice_gives_different_hashes()
    {
        Assert.NotEqual(_service.HashPassword("Reception2026"), _service.HashPassword("Reception2026"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64!")]
    [InlineData("AAAA")]
    public void Garbage_hashes_never_verify_and_never_throw(string? hash)
    {
        Assert.False(_service.VerifyPassword("Reception2026", hash));
    }

    [Fact]
    public void An_empty_password_never_verifies()
    {
        Assert.False(_service.VerifyPassword("", _service.HashPassword("Reception2026")));
    }

    [Fact]
    public void A_hash_with_an_unknown_format_version_never_verifies()
    {
        var bytes = Convert.FromBase64String(_service.HashPassword("Reception2026"));
        bytes[0] = 2;

        Assert.False(_service.VerifyPassword("Reception2026", Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void A_hash_made_with_fewer_iterations_verifies_and_is_flagged_for_upgrade()
    {
        var old = OldHash("Reception2026", iterations: 1_000);

        Assert.True(_service.VerifyPassword("Reception2026", old));
        Assert.True(_service.NeedsRehash(old));
        Assert.False(_service.NeedsRehash(_service.HashPassword("Reception2026")));
    }

    [Fact]
    public void An_undecodable_hash_is_flagged_for_upgrade()
    {
        Assert.True(_service.NeedsRehash("not-base64!"));
    }

    [Fact]
    public void Hashing_a_blank_password_is_refused()
    {
        Assert.ThrowsAny<ArgumentException>(() => _service.HashPassword("  "));
    }

    [Fact]
    public void Spending_verification_time_costs_about_as_much_as_a_real_verification()
    {
        var hash = _service.HashPassword("Reception2026");
        _service.SpendVerificationTime("warm-up");

        var verify = Measure(() => _service.VerifyPassword("wrong-password", hash));
        var spend = Measure(() => _service.SpendVerificationTime("wrong-password"));

        Assert.True(spend > verify * 0.5, $"Equaliser took {spend}ms against {verify}ms for a real verification.");
    }

    [Fact]
    public void Password_strength_follows_the_shared_policy()
    {
        Assert.True(_service.ValidateStrength("Reception2026").IsValid);
        Assert.False(_service.ValidateStrength("short").IsValid);
        Assert.False(_service.ValidateStrength(null).IsValid);
    }

    private static double Measure(Action action)
    {
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 3; i++)
            action();
        return watch.Elapsed.TotalMilliseconds / 3;
    }

    // Same stored layout as PasswordService: [version 1][iterations 4, big-endian][salt 16][subkey 32].
    private static string OldHash(string password, int iterations)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        var bytes = new byte[53];
        bytes[0] = 1;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(1, 4), iterations);
        salt.CopyTo(bytes.AsSpan(5));
        subkey.CopyTo(bytes.AsSpan(21));
        return Convert.ToBase64String(bytes);
    }
}

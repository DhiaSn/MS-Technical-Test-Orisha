namespace MS.SS.Core.Security.Services;

/// <summary>A real hash of a throwaway value, computed once, that <see cref="PasswordService.SpendVerificationTime"/> verifies against.</summary>
internal static class TimingEqualizerHash
{
    public static readonly string Value = new PasswordService().HashPassword("ms-timing-equalizer");
}

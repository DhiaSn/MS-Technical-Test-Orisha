namespace MS.SS.Core.SharedKernel.Validators;

public static class UsernameValidator
{
    public const int MaxLength = 50;

    /// <summary>Lowercase letters, digits, '.', '_' and '-'. Callers normalise first.</summary>
    public static bool IsValid(string? username)
    {
        if (string.IsNullOrEmpty(username) || username.Length > MaxLength)
            return false;

        return username.All(IsAllowed);
    }

    public static string Normalize(string username) => username.Trim().ToLowerInvariant();

    private static bool IsAllowed(char c) =>
        c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-';
}

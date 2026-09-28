namespace MS.SS.Core.Security.Models.Tokens;

public sealed record AccessToken(string Token, DateTime ExpiresAt)
{
    public override string ToString() => $"{nameof(AccessToken)} {{ {nameof(ExpiresAt)} = {ExpiresAt:O} }}";
}

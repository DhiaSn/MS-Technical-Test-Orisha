namespace MS.SS.Core.Modules.Identity.Application.Dtos;

public sealed record SignInRequest(string? Username, string? Password)
{
    public override string ToString() => $"{nameof(SignInRequest)} {{ {nameof(Username)} = {Username} }}";
}

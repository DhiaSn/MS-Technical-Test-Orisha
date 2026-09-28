namespace MS.SS.Core.Modules.Identity.Application.Dtos;

public sealed record RegisterRequest(string? Username, string? DisplayName, string? Password)
{
    public override string ToString() =>
        $"{nameof(RegisterRequest)} {{ {nameof(Username)} = {Username}, {nameof(DisplayName)} = {DisplayName} }}";
}

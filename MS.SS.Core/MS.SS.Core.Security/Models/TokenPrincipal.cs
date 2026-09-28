namespace MS.SS.Core.Security.Models;

/// <summary>Everything needed to mint a token for a signed-in user.</summary>
public sealed record TokenPrincipal(Guid UserId, string Role);

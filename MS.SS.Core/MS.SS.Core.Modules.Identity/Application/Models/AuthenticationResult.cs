using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Security.Models.Tokens;

namespace MS.SS.Core.Modules.Identity.Application.Models;

/// <summary>The body to return and the token the endpoint puts in the cookie, kept apart so the token never reaches the body.</summary>
public sealed record AuthenticationResult(SessionResponse Response, AccessToken AccessToken);

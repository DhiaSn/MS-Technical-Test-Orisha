using MS.SS.Core.Security.Models;
using MS.SS.Core.Security.Models.Tokens;

namespace MS.SS.Core.Security.Interfaces;

public interface ITokenService
{
    AccessToken CreateAccessToken(TokenPrincipal principal);
}

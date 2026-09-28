using MS.SS.Core.Modules.Identity.Domain.Entities;

namespace MS.SS.Core.Modules.Identity.Application.Dtos;

public sealed record SessionResponse(
    Guid UserId, string Username, string DisplayName, string Role, DateTime AccessTokenExpiresAt)
{
    public static SessionResponse FromUser(User user, DateTime accessTokenExpiresAt) =>
        new(user.Id, user.Username, user.DisplayName, user.Role, accessTokenExpiresAt);
}

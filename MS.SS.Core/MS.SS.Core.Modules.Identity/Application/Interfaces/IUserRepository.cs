using MS.SS.Core.Modules.Identity.Domain.Entities;

namespace MS.SS.Core.Modules.Identity.Application.Interfaces;

public interface IUserRepository
{
    /// <summary>Name of the unique index on the username, so callers can tell its violation from any other.</summary>
    const string UniqueUsernameIndex = "ux_users_username";

    /// <summary>Tracked, so the caller can record the login and save.</summary>
    Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken);

    void Add(User user);
}

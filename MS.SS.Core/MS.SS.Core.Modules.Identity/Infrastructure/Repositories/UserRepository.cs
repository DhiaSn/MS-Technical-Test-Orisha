using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Domain.Entities;

namespace MS.SS.Core.Modules.Identity.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        context.Set<User>().FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        context.Set<User>().AnyAsync(u => u.Username == normalizedUsername, cancellationToken);

    public void Add(User user) => context.Set<User>().Add(user);
}

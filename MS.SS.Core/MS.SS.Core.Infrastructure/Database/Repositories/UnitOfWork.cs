using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.SharedKernel.Interfaces;

namespace MS.SS.Core.Infrastructure.Database.Repositories;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}

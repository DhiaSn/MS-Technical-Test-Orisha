using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.SharedKernel.Models;

namespace MS.SS.Core.Infrastructure.Database.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider clock) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var moduleAssembly in ModuleAssemblyLoader.LoadAll())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(moduleAssembly);
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.StampCreated(now);
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

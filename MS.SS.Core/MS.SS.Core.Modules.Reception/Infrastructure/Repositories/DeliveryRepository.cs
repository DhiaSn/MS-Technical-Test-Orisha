using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Repositories;

public sealed class DeliveryRepository(AppDbContext context) : IDeliveryRepository
{
    public Task<Delivery?> FindAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        WithGraph().AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

    public Task<Delivery?> FindForUpdateAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

    public Task<Delivery?> FindCurrentAsync(CancellationToken cancellationToken) =>
        WithGraph().AsNoTracking()
            .OrderBy(d => d.CreatedAt).ThenBy(d => d.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        context.Set<Delivery>().AnyAsync(cancellationToken);

    public void Add(Delivery delivery) => context.Set<Delivery>().Add(delivery);

    private IQueryable<Delivery> WithGraph() =>
        context.Set<Delivery>()
            .Include(d => d.Pallets)
            .ThenInclude(p => p.Cartons)
            .ThenInclude(c => c.Products)
            .AsSplitQuery();
}

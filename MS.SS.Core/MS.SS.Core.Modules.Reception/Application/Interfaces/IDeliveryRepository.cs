using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Application.Interfaces;

public interface IDeliveryRepository
{
    /// <summary>The whole aggregate, untracked: for queries.</summary>
    Task<Delivery?> FindAsync(Guid deliveryId, CancellationToken cancellationToken);

    /// <summary>The whole aggregate, tracked: for commands.</summary>
    Task<Delivery?> FindForUpdateAsync(Guid deliveryId, CancellationToken cancellationToken);

    /// <summary>The oldest delivery by creation time, untracked.</summary>
    Task<Delivery?> FindCurrentAsync(CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    void Add(Delivery delivery);
}

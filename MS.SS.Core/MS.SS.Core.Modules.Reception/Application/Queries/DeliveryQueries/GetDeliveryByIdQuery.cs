using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Queries.DeliveryQueries;

public sealed record GetDeliveryByIdQuery(Guid DeliveryId);

public sealed class GetDeliveryByIdQueryHandler(IDeliveryRepository deliveries)
{
    public async Task<Result<DeliveryResponse>> Handle(GetDeliveryByIdQuery query, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FindAsync(query.DeliveryId, cancellationToken);

        return delivery is null ? DeliveryErrors.NotFound() : DeliveryResponse.FromEntity(delivery);
    }
}

using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Queries.DeliveryQueries;

public sealed record GetCurrentDeliveryQuery;

public sealed class GetCurrentDeliveryQueryHandler(IDeliveryRepository deliveries)
{
    public async Task<Result<DeliveryResponse>> Handle(GetCurrentDeliveryQuery query, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FindCurrentAsync(cancellationToken);

        return delivery is null ? DeliveryErrors.NotFound() : DeliveryResponse.FromEntity(delivery);
    }
}

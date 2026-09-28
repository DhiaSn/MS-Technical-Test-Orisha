using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;

internal static class DeliveryMutation
{
    public static async Task<Result<DeliveryResponse>> ApplyAsync(
        IDeliveryRepository deliveries,
        IUnitOfWork unitOfWork,
        Guid deliveryId,
        Func<Delivery, Result> mutate,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FindForUpdateAsync(deliveryId, cancellationToken);
        if (delivery is null) return DeliveryErrors.NotFound();

        var outcome = mutate(delivery);
        if (outcome.IsFailure) return outcome.Exception;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeliveryResponse.FromEntity(delivery);
    }
}

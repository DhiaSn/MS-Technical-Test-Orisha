using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;

public sealed record SetProductReceivedQuantityCommand(Guid DeliveryId, Guid ProductId, int? ReceivedQuantity);

public sealed class SetProductReceivedQuantityHandler(IDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task<Result<DeliveryResponse>> Handle(
        SetProductReceivedQuantityCommand command, CancellationToken cancellationToken)
    {
        if (command.ReceivedQuantity is not { } quantity) return DeliveryErrors.Required("receivedQuantity");

        return await DeliveryMutation.ApplyAsync(
            deliveries, unitOfWork, command.DeliveryId,
            delivery => delivery.SetProductReceivedQuantity(command.ProductId, quantity), cancellationToken);
    }
}

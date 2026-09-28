using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;

public sealed record SetProductValidationCommand(Guid DeliveryId, Guid ProductId, bool? Validated);

public sealed class SetProductValidationHandler(IDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task<Result<DeliveryResponse>> Handle(SetProductValidationCommand command, CancellationToken cancellationToken)
    {
        if (command.Validated is not { } validated) return DeliveryErrors.Required("validated");

        return await DeliveryMutation.ApplyAsync(
            deliveries, unitOfWork, command.DeliveryId,
            delivery => delivery.SetProductValidated(command.ProductId, validated), cancellationToken);
    }
}

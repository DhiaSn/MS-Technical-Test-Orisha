using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;

public sealed record SetCartonValidationCommand(Guid DeliveryId, Guid CartonId, bool? Validated);

public sealed class SetCartonValidationHandler(IDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task<Result<DeliveryResponse>> Handle(SetCartonValidationCommand command, CancellationToken cancellationToken)
    {
        if (command.Validated is not { } validated) return DeliveryErrors.Required("validated");

        return await DeliveryMutation.ApplyAsync(
            deliveries, unitOfWork, command.DeliveryId,
            delivery => delivery.SetCartonValidated(command.CartonId, validated), cancellationToken);
    }
}

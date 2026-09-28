using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;

public sealed record SetPalletValidationCommand(Guid DeliveryId, Guid PalletId, bool? Validated);

public sealed class SetPalletValidationHandler(IDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task<Result<DeliveryResponse>> Handle(SetPalletValidationCommand command, CancellationToken cancellationToken)
    {
        if (command.Validated is not { } validated) return DeliveryErrors.Required("validated");

        return await DeliveryMutation.ApplyAsync(
            deliveries, unitOfWork, command.DeliveryId,
            delivery => delivery.SetPalletValidated(command.PalletId, validated), cancellationToken);
    }
}

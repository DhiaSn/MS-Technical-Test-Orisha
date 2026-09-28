using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;

namespace MS.SS.Core.Modules.Reception.Application.Dtos;

public sealed record DeliveryResponse(
    Guid Id,
    string OrderId,
    ValidationStatus Status,
    ProgressResponse Progress,
    IReadOnlyList<PalletResponse> Pallets)
{
    public static DeliveryResponse FromEntity(Delivery delivery) => new(
        delivery.Id,
        delivery.OrderNumber,
        delivery.Status,
        ProgressResponse.From(delivery.Progress),
        delivery.Pallets.OrderBy(p => p.Position).Select(PalletResponse.FromEntity).ToList());
}

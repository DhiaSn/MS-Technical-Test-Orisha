using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;

namespace MS.SS.Core.Modules.Reception.Application.Dtos;

public sealed record PalletResponse(
    Guid Id,
    string Code,
    ValidationStatus Status,
    ProgressResponse Progress,
    IReadOnlyList<CartonResponse> Cartons)
{
    public static PalletResponse FromEntity(Pallet pallet) => new(
        pallet.Id,
        pallet.Code,
        pallet.Status,
        ProgressResponse.From(pallet.Progress),
        pallet.Cartons.OrderBy(c => c.Position).Select(CartonResponse.FromEntity).ToList());
}

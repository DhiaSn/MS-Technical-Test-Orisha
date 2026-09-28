using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;

namespace MS.SS.Core.Modules.Reception.Application.Dtos;

public sealed record CartonResponse(
    Guid Id,
    string Code,
    ValidationStatus Status,
    ProgressResponse Progress,
    IReadOnlyList<ProductLineResponse> Products)
{
    public static CartonResponse FromEntity(Carton carton) => new(
        carton.Id,
        carton.Code,
        carton.Status,
        ProgressResponse.From(carton.Progress),
        carton.Products.OrderBy(p => p.Position).Select(ProductLineResponse.FromEntity).ToList());
}

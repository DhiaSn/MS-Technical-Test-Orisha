using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;

namespace MS.SS.Core.Modules.Reception.Application.Dtos;

public sealed record ProductLineResponse(
    Guid Id,
    string Reference,
    string Name,
    string Color,
    string Size,
    int ExpectedQuantity,
    int ReceivedQuantity,
    ValidationStatus Status)
{
    public static ProductLineResponse FromEntity(ProductLine line) => new(
        line.Id, line.Reference, line.Name, line.Color, line.Size,
        line.ExpectedQuantity, line.ReceivedQuantity, line.Status);
}

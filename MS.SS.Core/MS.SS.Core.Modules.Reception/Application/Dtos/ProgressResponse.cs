using MS.SS.Core.Modules.Reception.Domain.Models;

namespace MS.SS.Core.Modules.Reception.Application.Dtos;

public sealed record ProgressResponse(int ReceivedUnits, int ExpectedUnits)
{
    public static ProgressResponse From(ReceptionProgress progress) => new(progress.ReceivedUnits, progress.ExpectedUnits);
}

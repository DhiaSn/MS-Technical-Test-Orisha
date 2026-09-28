using MS.SS.Core.Modules.Reception.Domain.Enums;

namespace MS.SS.Core.Modules.Reception.Domain.Models;

public readonly record struct ReceptionProgress(int ReceivedUnits, int ExpectedUnits)
{
    // Received is checked first so a node with nothing expected reads "none", never a vacuous "all".
    public ValidationStatus Status =>
        ReceivedUnits == 0 ? ValidationStatus.None
        : ReceivedUnits == ExpectedUnits ? ValidationStatus.All
        : ValidationStatus.Partial;

    public static ReceptionProgress Sum(IEnumerable<ReceptionProgress> items) =>
        items.Aggregate(new ReceptionProgress(0, 0),
            (total, item) => new(total.ReceivedUnits + item.ReceivedUnits, total.ExpectedUnits + item.ExpectedUnits));
}

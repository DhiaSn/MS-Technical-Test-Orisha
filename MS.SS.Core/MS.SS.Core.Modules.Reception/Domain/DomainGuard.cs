using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Modules.Reception.Domain;

internal static class DomainGuard
{
    public static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidEntityStateException(ErrorCodes.InvalidState, $"{field} is required.");

        return value.Trim();
    }
}

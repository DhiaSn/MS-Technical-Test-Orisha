using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Modules.Reception.Application;

internal static class DeliveryErrors
{
    public static NotFoundException NotFound() =>
        new(ErrorCodes.DeliveryNotFound, "Delivery not found.");

    public static ValidationFailedException Required(string field) =>
        new(field, ErrorCodes.FieldRequired, $"The field '{field}' is required.");
}

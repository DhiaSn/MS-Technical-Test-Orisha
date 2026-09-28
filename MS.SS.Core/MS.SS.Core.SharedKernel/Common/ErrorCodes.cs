namespace MS.SS.Core.SharedKernel.Common;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation.failed";
    public const string RequestInvalid = "request.invalid";
    public const string UnsupportedMediaType = "request.unsupported_media_type";
    public const string RequestTooLarge = "request.too_large";
    public const string NotFound = "resource.not_found";
    public const string InvalidState = "state.invalid";
    public const string InternalError = "server.error";
    public const string FieldRequired = "field.required";

    public const string DeliveryNotFound = "reception.delivery_not_found";
    public const string PalletNotFound = "reception.pallet_not_found";
    public const string CartonNotFound = "reception.carton_not_found";
    public const string ProductNotFound = "reception.product_not_found";
    public const string QuantityOutOfRange = "reception.quantity_out_of_range";
    public const string ExpectedQuantityNotPositive = "reception.expected_quantity_not_positive";
}

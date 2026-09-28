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
    public const string FieldTooLong = "field.too_long";

    public const string Unauthenticated = "auth.unauthenticated";
    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string Forbidden = "access.forbidden";
    public const string RateLimited = "rate_limit.exceeded";
    public const string CsrfHeaderMissing = "csrf.header_missing";
    public const string CsrfOriginNotAllowed = "csrf.origin_not_allowed";
    public const string Conflict = "resource.conflict";
    public const string UsernameInvalid = "username.invalid";
    public const string UsernameTaken = "account.username_taken";
    public const string PasswordRequired = "password.required";
    public const string PasswordTooShort = "password.too_short";
    public const string PasswordMissingUppercase = "password.missing_uppercase";
    public const string PasswordMissingLowercase = "password.missing_lowercase";
    public const string PasswordMissingDigit = "password.missing_digit";

    public const string DeliveryNotFound = "reception.delivery_not_found";
    public const string PalletNotFound = "reception.pallet_not_found";
    public const string CartonNotFound = "reception.carton_not_found";
    public const string ProductNotFound = "reception.product_not_found";
    public const string QuantityOutOfRange = "reception.quantity_out_of_range";
    public const string ExpectedQuantityNotPositive = "reception.expected_quantity_not_positive";
}

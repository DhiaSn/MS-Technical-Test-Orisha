namespace MS.SS.Core.SharedKernel.Common;

/// <summary>
/// One field-level failure: a stable code for the client, a fallback message, and the values the
/// message interpolates (bounds, limits).
/// </summary>
public sealed record ValidationError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Parameters = null);

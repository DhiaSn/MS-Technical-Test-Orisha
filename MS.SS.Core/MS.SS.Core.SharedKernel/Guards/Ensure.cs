using System.Globalization;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.SharedKernel.Guards;

/// <summary>
/// Collects field-level validation failures and throws them as one
/// <see cref="ValidationFailedException"/>, so a round-trip reports every bad field at once
/// rather than one per submission.
/// </summary>
public sealed class Ensure
{
    private readonly Dictionary<string, List<ValidationError>> _errors = [];

    public static Ensure That() => new();

    public bool HasErrors => _errors.Count > 0;

    public Ensure NotNullOrWhiteSpace(
        string field, string? value, string message, string code = ErrorCodes.FieldRequired)
    {
        if (string.IsNullOrWhiteSpace(value)) Fail(field, new ValidationError(code, message));
        return this;
    }

    public Ensure MaxLength(
        string field, string? value, int maxLength, string message, string code = ErrorCodes.FieldTooLong)
    {
        if (value is not null && value.Length > maxLength)
        {
            Fail(field, new ValidationError(code, message, Parameters(("max", maxLength))));
        }

        return this;
    }

    /// <summary>Records an already-built failure, for rules that live outside this class such as password strength.</summary>
    public Ensure Fail(string field, ValidationError error)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(error);
        return this;
    }

    public ValidationFailedException? AsException() =>
        HasErrors
            ? new ValidationFailedException(
                _errors.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<ValidationError>)kv.Value.ToArray()))
            : null;

    public void ThrowIfInvalid()
    {
        var exception = AsException();
        if (exception is not null) throw exception;
    }

    private static Dictionary<string, string> Parameters(params (string Name, IFormattable Value)[] values) =>
        values.ToDictionary(v => v.Name, v => v.Value.ToString(null, CultureInfo.InvariantCulture));
}

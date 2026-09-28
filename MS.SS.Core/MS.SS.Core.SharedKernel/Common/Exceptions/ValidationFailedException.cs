namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>One or more field-level validation failures, kept per field so the client can attach each message to its input.</summary>
public class ValidationFailedException : Exception, ICodedException
{
    public ValidationFailedException(IReadOnlyDictionary<string, IReadOnlyList<ValidationError>> failures)
        : base("One or more validation errors occurred.")
        => Failures = failures;

    public ValidationFailedException(string field, string code, string message)
        : this(new Dictionary<string, IReadOnlyList<ValidationError>>
        {
            [field] = [new ValidationError(code, message)]
        })
    {
    }

    public string Code => ErrorCodes.ValidationFailed;

    public IReadOnlyDictionary<string, IReadOnlyList<ValidationError>> Failures { get; }

    /// <summary>The fallback messages per field, in the shape of the ProblemDetails <c>errors</c> member.</summary>
    public IReadOnlyDictionary<string, string[]> Errors =>
        Failures.ToDictionary(kv => kv.Key, kv => kv.Value.Select(e => e.Message).ToArray());
}

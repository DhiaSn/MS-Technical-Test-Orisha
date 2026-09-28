namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>An entity was asked to move into a state its invariants forbid. Maps to 400.</summary>
public class InvalidEntityStateException : Exception, ICodedException
{
    public InvalidEntityStateException() : this(ErrorCodes.InvalidState, string.Empty) { }

    public InvalidEntityStateException(string message) : this(ErrorCodes.InvalidState, message) { }

    public InvalidEntityStateException(string code, string message) : base(message) => Code = code;

    public InvalidEntityStateException(string message, Exception innerException) : base(message, innerException)
        => Code = ErrorCodes.InvalidState;

    public string Code { get; }
}

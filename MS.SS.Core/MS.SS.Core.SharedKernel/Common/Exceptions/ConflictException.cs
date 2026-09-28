namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>A uniqueness rule was broken, such as a username already taken. Maps to 409.</summary>
public class ConflictException : Exception, ICodedException
{
    public ConflictException(string message) : this(ErrorCodes.Conflict, message) { }

    public ConflictException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}

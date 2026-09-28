namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>The addressed resource does not exist. Maps to 404.</summary>
public class NotFoundException : KeyNotFoundException, ICodedException
{
    public NotFoundException(string message) : this(ErrorCodes.NotFound, message) { }

    public NotFoundException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}

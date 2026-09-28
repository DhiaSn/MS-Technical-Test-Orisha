namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>An exception that carries the stable code reported to API clients.</summary>
public interface ICodedException
{
    string Code { get; }
}

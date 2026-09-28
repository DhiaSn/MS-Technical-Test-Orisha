namespace MS.SS.Core.SharedKernel.Common.Exceptions;

/// <summary>The caller is not signed in, or the credentials they presented are not valid. Maps to 401.</summary>
public class UnauthenticatedException : UnauthorizedAccessException, ICodedException
{
    public UnauthenticatedException(string message) : this(ErrorCodes.Unauthenticated, message) { }

    public UnauthenticatedException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}

using MS.SS.Core.API.Extensions;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.Tests.API.Extensions;

public sealed class ApiContractExtensionsTests
{
    [Theory]
    [InlineData(401, ErrorCodes.Unauthenticated)]
    [InlineData(403, ErrorCodes.Forbidden)]
    [InlineData(404, ErrorCodes.NotFound)]
    [InlineData(409, ErrorCodes.Conflict)]
    [InlineData(413, ErrorCodes.RequestTooLarge)]
    [InlineData(415, ErrorCodes.UnsupportedMediaType)]
    [InlineData(429, ErrorCodes.RateLimited)]
    [InlineData(500, ErrorCodes.InternalError)]
    [InlineData(503, ErrorCodes.InternalError)]
    [InlineData(400, ErrorCodes.RequestInvalid)]
    [InlineData(405, ErrorCodes.RequestInvalid)]
    public void BodilessErrorStatus_MapsToItsCode(int status, string code)
    {
        Assert.Equal(code, ApiContractExtensions.CodeForStatus(status));
    }
}

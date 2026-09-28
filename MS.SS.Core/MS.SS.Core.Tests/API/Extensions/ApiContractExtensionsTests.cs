using MS.SS.Core.API.Extensions;
using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.Tests.API.Extensions;

public sealed class ApiContractExtensionsTests
{
    [Theory]
    [InlineData(404, ErrorCodes.NotFound)]
    [InlineData(500, ErrorCodes.InternalError)]
    [InlineData(503, ErrorCodes.InternalError)]
    [InlineData(400, ErrorCodes.RequestInvalid)]
    [InlineData(405, ErrorCodes.RequestInvalid)]
    public void BodilessErrorStatus_MapsToItsCode(int status, string code)
    {
        Assert.Equal(code, ApiContractExtensions.CodeForStatus(status));
    }
}

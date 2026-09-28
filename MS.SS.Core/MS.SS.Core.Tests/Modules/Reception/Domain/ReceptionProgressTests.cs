using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;

namespace MS.SS.Core.Tests.Modules.Reception.Domain;

public class ReceptionProgressTests
{
    [Theory]
    [InlineData(0, 0, ValidationStatus.None)]
    [InlineData(0, 10, ValidationStatus.None)]
    [InlineData(3, 10, ValidationStatus.Partial)]
    [InlineData(9, 10, ValidationStatus.Partial)]
    [InlineData(10, 10, ValidationStatus.All)]
    public void Status_is_derived_from_received_and_expected_units(int received, int expected, ValidationStatus status)
    {
        Assert.Equal(status, new ReceptionProgress(received, expected).Status);
    }

    [Fact]
    public void Sum_adds_received_and_expected_units_separately()
    {
        var total = ReceptionProgress.Sum([new(2, 10), new(5, 5), new(0, 1)]);

        Assert.Equal(new ReceptionProgress(7, 16), total);
    }
}

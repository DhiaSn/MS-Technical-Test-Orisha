using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Tests.Modules.Reception.Domain;

public class DeliveryQuantityTests
{
    [Theory]
    [InlineData(11)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void An_out_of_range_quantity_is_rejected_and_changes_nothing(int quantity)
    {
        var s = new SampleDelivery();
        s.Order.SetProductReceivedQuantity(s.A1.Id, 3);

        var result = s.Order.SetProductReceivedQuantity(s.A1.Id, quantity);

        var failure = Assert.IsType<ValidationFailedException>(result.Exception);
        var error = Assert.Single(failure.Failures["receivedQuantity"]);
        Assert.Equal(ErrorCodes.QuantityOutOfRange, error.Code);
        Assert.Equal("0", error.Parameters!["min"]);
        Assert.Equal("10", error.Parameters["max"]);
        Assert.Equal(3, s.A1.ReceivedQuantity);
        Assert.Equal(new ReceptionProgress(3, 43), s.Order.Progress);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void The_bounds_themselves_are_accepted(int quantity)
    {
        var s = new SampleDelivery();

        Assert.True(s.Order.SetProductReceivedQuantity(s.A1.Id, quantity).IsSuccess);
        Assert.Equal(quantity, s.A1.ReceivedQuantity);
    }
}

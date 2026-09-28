using MS.SS.Core.App.Seeding;
using MS.SS.Core.Modules.Reception.Domain.Models;

namespace MS.SS.Core.Tests.App;

public class DemoDeliveriesTests
{
    [Fact]
    public void The_demo_delivery_has_the_documented_structure_and_nothing_received()
    {
        var delivery = DemoDeliveries.CreateCmd2026();

        Assert.Equal("CMD-2026", delivery.OrderNumber);
        Assert.Equal(new[] { "PAL-01", "PAL-02", "PAL-03" }, delivery.Pallets.Select(p => p.Code));
        Assert.Equal(7, delivery.Pallets.Sum(p => p.Cartons.Count));
        Assert.Equal(15, delivery.Pallets.SelectMany(p => p.Cartons).Sum(c => c.Products.Count));
        Assert.Equal(new ReceptionProgress(0, 504), delivery.Progress);
        Assert.Equal(new[] { 300, 116, 88 }, delivery.Pallets.Select(p => p.Progress.ExpectedUnits));
    }
}

using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;

namespace MS.SS.Core.Tests.Modules.Reception.Domain;

public class DeliveryStructureTests
{
    [Fact]
    public void A_carton_without_products_is_none_not_all()
    {
        var order = Delivery.Create("CMD-EMPTY");
        var carton = order.AddPallet("PAL-01").AddCarton("CART-EMPTY");

        Assert.Equal(new ReceptionProgress(0, 0), carton.Progress);
        Assert.Equal(ValidationStatus.None, carton.Status);
        Assert.Equal(ValidationStatus.None, order.Status);
    }

    [Fact]
    public void The_expected_quantity_must_be_strictly_positive()
    {
        var carton = Delivery.Create("CMD-X").AddPallet("PAL-01").AddCarton("CART-01");

        var error = Assert.Throws<InvalidEntityStateException>(
            () => carton.AddProduct("REF", "Item", "Rouge", "M", 0));

        Assert.Equal(ErrorCodes.ExpectedQuantityNotPositive, error.Code);
    }

    [Fact]
    public void Codes_and_references_are_unique_within_their_parent()
    {
        var order = Delivery.Create("CMD-X");
        var pallet = order.AddPallet("PAL-01");
        var carton = pallet.AddCarton("CART-01");
        carton.AddProduct("REF", "Item", "Rouge", "M", 1);

        Assert.Throws<InvalidEntityStateException>(() => order.AddPallet("PAL-01"));
        Assert.Throws<InvalidEntityStateException>(() => pallet.AddCarton("CART-01"));
        Assert.Throws<InvalidEntityStateException>(() => carton.AddProduct("REF", "Other", "Noir", "L", 2));
        order.AddPallet("PAL-02").AddCarton("CART-01");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_order_number_is_required(string orderNumber)
    {
        Assert.Throws<InvalidEntityStateException>(() => Delivery.Create(orderNumber));
    }

    [Fact]
    public void Insertion_order_is_kept_as_position()
    {
        var order = Delivery.Create("CMD-X");
        var first = order.AddPallet("PAL-B");
        var second = order.AddPallet("PAL-A");

        Assert.Equal(new[] { 0, 1 }, new[] { first.Position, second.Position });
    }
}

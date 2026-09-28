using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Tests.Modules.Reception.Domain;

public class DeliveryCascadeTests
{
    [Fact]
    public void Validating_a_pallet_receives_every_product_under_it()
    {
        var s = new SampleDelivery();

        var result = s.Order.SetPalletValidated(s.Pallet1.Id, true);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 10, 5, 20 }, new[] { s.A1.ReceivedQuantity, s.A2.ReceivedQuantity, s.B1.ReceivedQuantity });
        Assert.Equal(0, s.C1.ReceivedQuantity);
        Assert.Equal(ValidationStatus.All, s.Pallet1.Status);
        Assert.Equal(ValidationStatus.None, s.Pallet2.Status);
        Assert.Equal(ValidationStatus.Partial, s.Order.Status);
    }

    [Fact]
    public void Validating_a_carton_receives_only_its_own_products()
    {
        var s = new SampleDelivery();

        s.Order.SetCartonValidated(s.CartonA.Id, true);

        Assert.Equal(ValidationStatus.All, s.CartonA.Status);
        Assert.Equal(ValidationStatus.None, s.CartonB.Status);
        Assert.Equal(ValidationStatus.Partial, s.Pallet1.Status);
    }

    [Fact]
    public void Validating_a_product_receives_its_full_expected_quantity()
    {
        var s = new SampleDelivery();

        s.Order.SetProductValidated(s.A1.Id, true);

        Assert.Equal(10, s.A1.ReceivedQuantity);
        Assert.Equal(ValidationStatus.All, s.A1.Status);
        Assert.Equal(ValidationStatus.Partial, s.CartonA.Status);
    }

    [Fact]
    public void Ticking_every_product_of_a_carton_one_by_one_validates_the_carton()
    {
        var s = new SampleDelivery();

        s.Order.SetProductValidated(s.A1.Id, true);
        s.Order.SetProductValidated(s.A2.Id, true);

        Assert.Equal(ValidationStatus.All, s.CartonA.Status);
        Assert.Equal(ValidationStatus.Partial, s.Pallet1.Status);
        Assert.Equal(ValidationStatus.Partial, s.Order.Status);
    }

    [Fact]
    public void Ticking_every_product_of_a_pallet_validates_the_pallet_and_the_delivery_when_it_is_the_last()
    {
        var s = new SampleDelivery();

        foreach (var product in new[] { s.A1, s.A2, s.B1, s.C1 })
            s.Order.SetProductValidated(product.Id, true);

        Assert.Equal(ValidationStatus.All, s.Pallet1.Status);
        Assert.Equal(ValidationStatus.All, s.Pallet2.Status);
        Assert.Equal(ValidationStatus.All, s.Order.Status);
        Assert.Equal(new ReceptionProgress(43, 43), s.Order.Progress);
    }

    [Fact]
    public void Unticking_a_product_makes_its_carton_and_pallet_partial()
    {
        var s = new SampleDelivery();
        s.Order.SetPalletValidated(s.Pallet1.Id, true);

        s.Order.SetProductValidated(s.A1.Id, false);

        Assert.Equal(ValidationStatus.None, s.A1.Status);
        Assert.Equal(ValidationStatus.Partial, s.CartonA.Status);
        Assert.Equal(ValidationStatus.All, s.CartonB.Status);
        Assert.Equal(ValidationStatus.Partial, s.Pallet1.Status);
        Assert.Equal(ValidationStatus.Partial, s.Order.Status);
    }

    [Fact]
    public void Unticking_the_only_received_product_of_a_carton_returns_it_to_none()
    {
        var s = new SampleDelivery();
        s.Order.SetProductValidated(s.A1.Id, true);

        s.Order.SetProductValidated(s.A1.Id, false);

        Assert.Equal(ValidationStatus.None, s.CartonA.Status);
        Assert.Equal(ValidationStatus.None, s.Order.Status);
    }

    [Fact]
    public void Unvalidating_a_pallet_resets_every_product_under_it_to_zero()
    {
        var s = new SampleDelivery();
        s.Order.SetPalletValidated(s.Pallet1.Id, true);

        s.Order.SetPalletValidated(s.Pallet1.Id, false);

        Assert.Equal(new[] { 0, 0, 0 }, new[] { s.A1.ReceivedQuantity, s.A2.ReceivedQuantity, s.B1.ReceivedQuantity });
        Assert.Equal(ValidationStatus.None, s.Pallet1.Status);
    }

    [Fact]
    public void Unvalidating_a_partially_received_carton_resets_it_to_zero()
    {
        var s = new SampleDelivery();
        s.Order.SetProductReceivedQuantity(s.A1.Id, 4);

        s.Order.SetCartonValidated(s.CartonA.Id, false);

        Assert.Equal(0, s.A1.ReceivedQuantity);
        Assert.Equal(ValidationStatus.None, s.CartonA.Status);
    }

    [Fact]
    public void Validating_a_partially_received_carton_completes_it_instead_of_toggling()
    {
        var s = new SampleDelivery();
        s.Order.SetProductReceivedQuantity(s.A1.Id, 3);

        s.Order.SetCartonValidated(s.CartonA.Id, true);

        Assert.Equal(new[] { 10, 5 }, new[] { s.A1.ReceivedQuantity, s.A2.ReceivedQuantity });
        Assert.Equal(ValidationStatus.All, s.CartonA.Status);
    }

    [Fact]
    public void Validating_twice_changes_nothing_and_still_succeeds()
    {
        var s = new SampleDelivery();
        s.Order.SetPalletValidated(s.Pallet1.Id, true);

        var second = s.Order.SetPalletValidated(s.Pallet1.Id, true);

        Assert.True(second.IsSuccess);
        Assert.Equal(new ReceptionProgress(35, 43), s.Order.Progress);
    }

    [Fact]
    public void Unvalidating_twice_changes_nothing_and_still_succeeds()
    {
        var s = new SampleDelivery();

        var result = s.Order.SetProductValidated(s.A1.Id, false);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ReceptionProgress(0, 43), s.Order.Progress);
    }

    [Fact]
    public void A_quantity_below_expected_makes_the_product_partial()
    {
        var s = new SampleDelivery();

        var result = s.Order.SetProductReceivedQuantity(s.A1.Id, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidationStatus.Partial, s.A1.Status);
        Assert.Equal(ValidationStatus.Partial, s.CartonA.Status);
        Assert.Equal(new ReceptionProgress(4, 43), s.Order.Progress);
    }

    [Fact]
    public void The_full_quantity_validates_the_product_and_can_complete_its_carton()
    {
        var s = new SampleDelivery();
        s.Order.SetProductValidated(s.A2.Id, true);

        s.Order.SetProductReceivedQuantity(s.A1.Id, 10);

        Assert.Equal(ValidationStatus.All, s.A1.Status);
        Assert.Equal(ValidationStatus.All, s.CartonA.Status);
    }

    [Fact]
    public void A_quantity_of_zero_returns_the_product_to_none()
    {
        var s = new SampleDelivery();
        s.Order.SetProductValidated(s.A1.Id, true);

        s.Order.SetProductReceivedQuantity(s.A1.Id, 0);

        Assert.Equal(ValidationStatus.None, s.A1.Status);
    }

    [Fact]
    public void Unknown_ids_are_reported_as_not_found_with_a_code_per_kind()
    {
        var s = new SampleDelivery();
        var unknown = Guid.NewGuid();

        Assert.Equal(ErrorCodes.PalletNotFound, CodeOf(s.Order.SetPalletValidated(unknown, true)));
        Assert.Equal(ErrorCodes.CartonNotFound, CodeOf(s.Order.SetCartonValidated(unknown, true)));
        Assert.Equal(ErrorCodes.ProductNotFound, CodeOf(s.Order.SetProductValidated(unknown, true)));
        Assert.Equal(ErrorCodes.ProductNotFound, CodeOf(s.Order.SetProductReceivedQuantity(unknown, 1)));
        Assert.Equal(new ReceptionProgress(0, 43), s.Order.Progress);
    }

    [Fact]
    public void An_id_of_the_wrong_kind_is_not_found()
    {
        var s = new SampleDelivery();

        var result = s.Order.SetPalletValidated(s.CartonA.Id, true);

        Assert.Equal(ErrorCodes.PalletNotFound, CodeOf(result));
        Assert.Equal(0, s.A1.ReceivedQuantity);
    }

    [Fact]
    public void Progress_sums_units_across_the_whole_delivery()
    {
        var s = new SampleDelivery();
        s.Order.SetProductReceivedQuantity(s.A1.Id, 4);
        s.Order.SetCartonValidated(s.CartonC.Id, true);

        Assert.Equal(new ReceptionProgress(12, 43), s.Order.Progress);
        Assert.Equal(new ReceptionProgress(4, 35), s.Pallet1.Progress);
        Assert.Equal(new ReceptionProgress(8, 8), s.Pallet2.Progress);
    }

    private static string CodeOf(Result result) =>
        Assert.IsAssignableFrom<ICodedException>(result.Exception).Code;
}

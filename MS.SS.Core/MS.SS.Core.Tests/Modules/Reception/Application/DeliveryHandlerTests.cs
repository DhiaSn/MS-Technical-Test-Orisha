using MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Application.Queries.DeliveryQueries;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Interfaces;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using NSubstitute;

namespace MS.SS.Core.Tests.Modules.Reception.Application;

public class DeliveryHandlerTests
{
    private readonly IDeliveryRepository _deliveries = Substitute.For<IDeliveryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SampleDelivery _s = new();

    public DeliveryHandlerTests()
    {
        _deliveries.FindForUpdateAsync(_s.Order.Id, Arg.Any<CancellationToken>()).Returns(_s.Order);
        _deliveries.FindAsync(_s.Order.Id, Arg.Any<CancellationToken>()).Returns(_s.Order);
    }

    [Fact]
    public async Task Validating_a_pallet_saves_once_and_returns_the_cascaded_delivery()
    {
        var handler = new SetPalletValidationHandler(_deliveries, _unitOfWork);

        var result = await handler.Handle(new(_s.Order.Id, _s.Pallet1.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ProgressResponse(35, 43), result.Value.Progress);
        Assert.Equal(ValidationStatus.All, result.Value.Pallets[0].Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validating_a_carton_saves_once_and_returns_the_updated_delivery()
    {
        var result = await new SetCartonValidationHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.CartonA.Id, true), CancellationToken.None);

        Assert.Equal(ValidationStatus.All, result.Value.Pallets[0].Cartons[0].Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validating_a_product_saves_once_and_returns_the_updated_delivery()
    {
        var result = await new SetProductValidationHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.A1.Id, true), CancellationToken.None);

        Assert.Equal(10, result.Value.Pallets[0].Cartons[0].Products[0].ReceivedQuantity);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Setting_a_quantity_saves_once_and_returns_a_partial_product()
    {
        var result = await new SetProductReceivedQuantityHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.A1.Id, 4), CancellationToken.None);

        var product = result.Value.Pallets[0].Cartons[0].Products[0];
        Assert.Equal((4, ValidationStatus.Partial), (product.ReceivedQuantity, product.Status));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_missing_validated_flag_is_a_validation_failure_and_touches_nothing()
    {
        var result = await new SetPalletValidationHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.Pallet1.Id, null), CancellationToken.None);

        var failure = Assert.IsType<ValidationFailedException>(result.Exception);
        Assert.Equal(ErrorCodes.FieldRequired, failure.Failures["validated"][0].Code);
        await _deliveries.DidNotReceiveWithAnyArgs().FindForUpdateAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_missing_received_quantity_is_a_validation_failure_and_touches_nothing()
    {
        var result = await new SetProductReceivedQuantityHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.A1.Id, null), CancellationToken.None);

        var failure = Assert.IsType<ValidationFailedException>(result.Exception);
        Assert.Equal(ErrorCodes.FieldRequired, failure.Failures["receivedQuantity"][0].Code);
        await _deliveries.DidNotReceiveWithAnyArgs().FindForUpdateAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task An_unknown_delivery_is_not_found_and_saves_nothing()
    {
        var result = await new SetPalletValidationHandler(_deliveries, _unitOfWork)
            .Handle(new(Guid.NewGuid(), _s.Pallet1.Id, true), CancellationToken.None);

        Assert.Equal(ErrorCodes.DeliveryNotFound, Assert.IsType<NotFoundException>(result.Exception).Code);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task A_node_of_another_delivery_is_not_found_and_saves_nothing()
    {
        var other = new SampleDelivery();

        var result = await new SetCartonValidationHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, other.CartonA.Id, true), CancellationToken.None);

        Assert.Equal(ErrorCodes.CartonNotFound, Assert.IsType<NotFoundException>(result.Exception).Code);
        Assert.Equal(0, other.A1.ReceivedQuantity);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task An_out_of_range_quantity_is_rejected_and_saves_nothing()
    {
        var result = await new SetProductReceivedQuantityHandler(_deliveries, _unitOfWork)
            .Handle(new(_s.Order.Id, _s.A1.Id, 11), CancellationToken.None);

        Assert.IsType<ValidationFailedException>(result.Exception);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Get_delivery_by_id_maps_the_aggregate_in_packing_list_order()
    {
        var result = await new GetDeliveryByIdQueryHandler(_deliveries)
            .Handle(new(_s.Order.Id), CancellationToken.None);

        Assert.Equal(new[] { "PAL-01", "PAL-02" }, result.Value.Pallets.Select(p => p.Code));
        Assert.Equal(new[] { "CART-A", "CART-B" }, result.Value.Pallets[0].Cartons.Select(c => c.Code));
    }

    [Fact]
    public async Task Get_delivery_by_id_is_not_found_for_an_unknown_id()
    {
        var result = await new GetDeliveryByIdQueryHandler(_deliveries)
            .Handle(new(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorCodes.DeliveryNotFound, Assert.IsType<NotFoundException>(result.Exception).Code);
    }

    [Fact]
    public async Task Get_current_delivery_maps_the_oldest_delivery()
    {
        _deliveries.FindCurrentAsync(Arg.Any<CancellationToken>()).Returns(_s.Order);

        var result = await new GetCurrentDeliveryQueryHandler(_deliveries)
            .Handle(new GetCurrentDeliveryQuery(), CancellationToken.None);

        Assert.Equal(_s.Order.OrderNumber, result.Value.OrderId);
    }

    [Fact]
    public async Task Get_current_delivery_is_not_found_when_none_exists()
    {
        _deliveries.FindCurrentAsync(Arg.Any<CancellationToken>()).Returns((Delivery?)null);

        var result = await new GetCurrentDeliveryQueryHandler(_deliveries)
            .Handle(new GetCurrentDeliveryQuery(), CancellationToken.None);

        Assert.Equal(ErrorCodes.DeliveryNotFound, Assert.IsType<NotFoundException>(result.Exception).Code);
    }
}

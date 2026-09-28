using Microsoft.AspNetCore.Mvc;
using MS.SS.Core.API.Extensions;
using MS.SS.Core.Modules.Reception.Application.Commands.DeliveryCommands;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Application.Queries.DeliveryQueries;
using MS.SS.Core.SharedKernel.Results;
using Wolverine;

namespace MS.SS.Core.API.Endpoints.Reception;

public static class DeliveryEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reception/deliveries")
            .WithTags("Reception - Deliveries")
            .RequireAuthorization();

        MapGetCurrent(group);
        MapGetById(group);
        MapSetPalletValidation(group);
        MapSetCartonValidation(group);
        MapSetProductValidation(group);
        MapSetProductReceivedQuantity(group);

        return app;
    }

    private static void MapGetCurrent(RouteGroupBuilder group)
    {
        group.MapGet("/current", async (IMessageBus bus, CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(new GetCurrentDeliveryQuery(), ct)).ToHttpResult())
        .WithName("GetCurrentDelivery")
        .WithSummary("The delivery being received")
        .WithDescription(
            "The oldest delivery, with its pallets, cartons and product lines. Every status and progress " +
            "figure is derived server-side from the received quantities.")
        .Produces<DeliveryResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapGetById(RouteGroupBuilder group)
    {
        group.MapGet("/{deliveryId:guid}", async (Guid deliveryId, IMessageBus bus, CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(new GetDeliveryByIdQuery(deliveryId), ct)).ToHttpResult())
        .WithName("GetDeliveryById")
        .WithSummary("A delivery by id")
        .Produces<DeliveryResponse>()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapSetPalletValidation(RouteGroupBuilder group)
    {
        group.MapPut("/{deliveryId:guid}/pallets/{palletId:guid}/validation", async (
            Guid deliveryId,
            Guid palletId,
            [FromBody] SetValidationRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(
                new SetPalletValidationCommand(deliveryId, palletId, request.Validated), ct)).ToHttpResult())
        .WithName("SetPalletValidation")
        .WithSummary("Validate or un-validate a pallet")
        .WithDescription(
            "validated=true receives every product under the pallet in full; validated=false resets them " +
            "to zero. Idempotent. Answers with the whole updated delivery.")
        .Produces<DeliveryResponse>()
        .ProducesValidationProblem()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapSetCartonValidation(RouteGroupBuilder group)
    {
        group.MapPut("/{deliveryId:guid}/cartons/{cartonId:guid}/validation", async (
            Guid deliveryId,
            Guid cartonId,
            [FromBody] SetValidationRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(
                new SetCartonValidationCommand(deliveryId, cartonId, request.Validated), ct)).ToHttpResult())
        .WithName("SetCartonValidation")
        .WithSummary("Validate or un-validate a carton")
        .WithDescription(
            "validated=true receives every product in the carton in full; validated=false resets them " +
            "to zero. Idempotent. Answers with the whole updated delivery.")
        .Produces<DeliveryResponse>()
        .ProducesValidationProblem()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapSetProductValidation(RouteGroupBuilder group)
    {
        group.MapPut("/{deliveryId:guid}/products/{productId:guid}/validation", async (
            Guid deliveryId,
            Guid productId,
            [FromBody] SetValidationRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(
                new SetProductValidationCommand(deliveryId, productId, request.Validated), ct)).ToHttpResult())
        .WithName("SetProductValidation")
        .WithSummary("Validate or un-validate a product line")
        .WithDescription(
            "validated=true receives the expected quantity; validated=false resets the line to zero. " +
            "Idempotent. Answers with the whole updated delivery.")
        .Produces<DeliveryResponse>()
        .ProducesValidationProblem()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapSetProductReceivedQuantity(RouteGroupBuilder group)
    {
        group.MapPut("/{deliveryId:guid}/products/{productId:guid}/received-quantity", async (
            Guid deliveryId,
            Guid productId,
            [FromBody] SetReceivedQuantityRequest request,
            IMessageBus bus,
            CancellationToken ct) =>
            (await bus.InvokeAsync<Result<DeliveryResponse>>(
                new SetProductReceivedQuantityCommand(deliveryId, productId, request.ReceivedQuantity), ct)).ToHttpResult())
        .WithName("SetProductReceivedQuantity")
        .WithSummary("Set the received quantity of a product line")
        .WithDescription(
            "An integer between 0 and the expected quantity. Below the expected quantity the line is " +
            "partially received; equal to it, the line is received; 0, not received.")
        .Produces<DeliveryResponse>()
        .ProducesValidationProblem()
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

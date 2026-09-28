using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Models;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Domain.Entities;

public sealed class Delivery : AuditableEntity
{
    private readonly List<Pallet> _pallets = [];

    public string OrderNumber { get; private set; } = string.Empty;

    public IReadOnlyList<Pallet> Pallets => _pallets;

    public ReceptionProgress Progress => ReceptionProgress.Sum(_pallets.Select(p => p.Progress));

    public ValidationStatus Status => Progress.Status;

    private Delivery() { }

    public static Delivery Create(string orderNumber) =>
        new() { Id = Guid.NewGuid(), OrderNumber = DomainGuard.Required(orderNumber, nameof(orderNumber)) };

    public Pallet AddPallet(string code)
    {
        var pallet = new Pallet(code, _pallets.Count);

        if (_pallets.Any(p => p.Code == pallet.Code))
            throw new InvalidEntityStateException(
                ErrorCodes.InvalidState, $"Pallet {pallet.Code} already exists in delivery {OrderNumber}.");

        _pallets.Add(pallet);
        return pallet;
    }

    public Result SetPalletValidated(Guid palletId, bool validated)
    {
        var pallet = _pallets.FirstOrDefault(p => p.Id == palletId);
        if (pallet is null) return new NotFoundException(ErrorCodes.PalletNotFound, "Pallet not found.");

        pallet.SetValidated(validated);
        return Result.Success();
    }

    public Result SetCartonValidated(Guid cartonId, bool validated)
    {
        var carton = _pallets.SelectMany(p => p.Cartons).FirstOrDefault(c => c.Id == cartonId);
        if (carton is null) return new NotFoundException(ErrorCodes.CartonNotFound, "Carton not found.");

        carton.SetValidated(validated);
        return Result.Success();
    }

    public Result SetProductValidated(Guid productId, bool validated)
    {
        var product = FindProduct(productId);
        if (product is null) return new NotFoundException(ErrorCodes.ProductNotFound, "Product not found.");

        product.SetValidated(validated);
        return Result.Success();
    }

    public Result SetProductReceivedQuantity(Guid productId, int quantity)
    {
        var product = FindProduct(productId);
        return product is null
            ? new NotFoundException(ErrorCodes.ProductNotFound, "Product not found.")
            : product.SetReceivedQuantity(quantity);
    }

    private ProductLine? FindProduct(Guid productId) =>
        _pallets.SelectMany(p => p.Cartons).SelectMany(c => c.Products).FirstOrDefault(p => p.Id == productId);
}

using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Models;

namespace MS.SS.Core.Modules.Reception.Domain.Entities;

public sealed class Carton : Entity
{
    private readonly List<ProductLine> _products = [];

    public string Code { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public IReadOnlyList<ProductLine> Products => _products;

    public ReceptionProgress Progress => ReceptionProgress.Sum(_products.Select(p => p.Progress));

    public ValidationStatus Status => Progress.Status;

    private Carton() { }

    internal Carton(string code, int position) : base(Guid.NewGuid())
    {
        Code = DomainGuard.Required(code, nameof(code));
        Position = position;
    }

    public ProductLine AddProduct(string reference, string name, string color, string size, int expectedQuantity)
    {
        var line = new ProductLine(reference, name, color, size, expectedQuantity, _products.Count);

        if (_products.Any(p => p.Reference == line.Reference))
            throw new InvalidEntityStateException(
                ErrorCodes.InvalidState, $"Reference {line.Reference} already exists in carton {Code}.");

        _products.Add(line);
        return line;
    }

    internal void SetValidated(bool validated)
    {
        foreach (var product in _products) product.SetValidated(validated);
    }
}

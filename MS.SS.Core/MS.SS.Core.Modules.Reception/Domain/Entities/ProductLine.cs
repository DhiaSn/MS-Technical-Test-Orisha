using System.Globalization;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Models;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Reception.Domain.Entities;

public sealed class ProductLine : Entity
{
    public string Reference { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Color { get; private set; } = string.Empty;

    public string Size { get; private set; } = string.Empty;

    public int ExpectedQuantity { get; private set; }

    public int ReceivedQuantity { get; private set; }

    public int Position { get; private set; }

    public ReceptionProgress Progress => new(ReceivedQuantity, ExpectedQuantity);

    public ValidationStatus Status => Progress.Status;

    private ProductLine() { }

    internal ProductLine(string reference, string name, string color, string size, int expectedQuantity, int position)
        : base(Guid.NewGuid())
    {
        if (expectedQuantity <= 0)
            throw new InvalidEntityStateException(
                ErrorCodes.ExpectedQuantityNotPositive, "The expected quantity must be strictly positive.");

        Reference = DomainGuard.Required(reference, nameof(reference));
        Name = DomainGuard.Required(name, nameof(name));
        Color = DomainGuard.Required(color, nameof(color));
        Size = DomainGuard.Required(size, nameof(size));
        ExpectedQuantity = expectedQuantity;
        Position = position;
    }

    internal void SetValidated(bool validated) => ReceivedQuantity = validated ? ExpectedQuantity : 0;

    internal Result SetReceivedQuantity(int quantity)
    {
        if (quantity < 0 || quantity > ExpectedQuantity)
        {
            var error = new ValidationError(
                ErrorCodes.QuantityOutOfRange,
                $"The received quantity must be between 0 and {ExpectedQuantity}.",
                new Dictionary<string, string>
                {
                    ["min"] = "0",
                    ["max"] = ExpectedQuantity.ToString(CultureInfo.InvariantCulture)
                });

            return new ValidationFailedException(new Dictionary<string, IReadOnlyList<ValidationError>>
            {
                ["receivedQuantity"] = [error]
            });
        }

        ReceivedQuantity = quantity;
        return Result.Success();
    }
}

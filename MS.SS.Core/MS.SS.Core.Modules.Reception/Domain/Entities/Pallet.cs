using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.SharedKernel.Common;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Models;

namespace MS.SS.Core.Modules.Reception.Domain.Entities;

public sealed class Pallet : Entity
{
    private readonly List<Carton> _cartons = [];

    public string Code { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public IReadOnlyList<Carton> Cartons => _cartons;

    public ReceptionProgress Progress => ReceptionProgress.Sum(_cartons.Select(c => c.Progress));

    public ValidationStatus Status => Progress.Status;

    private Pallet() { }

    internal Pallet(string code, int position) : base(Guid.NewGuid())
    {
        Code = DomainGuard.Required(code, nameof(code));
        Position = position;
    }

    public Carton AddCarton(string code)
    {
        var carton = new Carton(code, _cartons.Count);

        if (_cartons.Any(c => c.Code == carton.Code))
            throw new InvalidEntityStateException(
                ErrorCodes.InvalidState, $"Carton {carton.Code} already exists in pallet {Code}.");

        _cartons.Add(carton);
        return carton;
    }

    internal void SetValidated(bool validated)
    {
        foreach (var carton in _cartons) carton.SetValidated(validated);
    }
}

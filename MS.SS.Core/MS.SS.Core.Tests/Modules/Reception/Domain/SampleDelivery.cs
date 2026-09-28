using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Tests.Modules.Reception.Domain;

internal sealed class SampleDelivery
{
    // Unique per instance: integration tests persist these deliveries into one shared PostgreSQL container.
    public Delivery Order { get; } = Delivery.Create($"CMD-TEST-{Guid.NewGuid():N}");
    public Pallet Pallet1 { get; }
    public Pallet Pallet2 { get; }
    public Carton CartonA { get; }
    public Carton CartonB { get; }
    public Carton CartonC { get; }
    public ProductLine A1 { get; }
    public ProductLine A2 { get; }
    public ProductLine B1 { get; }
    public ProductLine C1 { get; }

    public SampleDelivery()
    {
        Pallet1 = Order.AddPallet("PAL-01");
        CartonA = Pallet1.AddCarton("CART-A");
        A1 = CartonA.AddProduct("REF-A1", "Item A1", "Rouge", "M", 10);
        A2 = CartonA.AddProduct("REF-A2", "Item A2", "Noir", "L", 5);
        CartonB = Pallet1.AddCarton("CART-B");
        B1 = CartonB.AddProduct("REF-B1", "Item B1", "Bleu", "U", 20);
        Pallet2 = Order.AddPallet("PAL-02");
        CartonC = Pallet2.AddCarton("CART-C");
        C1 = CartonC.AddProduct("REF-C1", "Item C1", "Vert", "S", 8);
    }
}

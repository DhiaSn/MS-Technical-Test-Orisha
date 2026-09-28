using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.App.Seeding;

public static class DemoDeliveries
{
    public static Delivery CreateCmd2026()
    {
        var delivery = Delivery.Create("CMD-2026");

        var pallet1 = delivery.AddPallet("PAL-01");
        var carton1A = pallet1.AddCarton("CART-01-A");
        carton1A.AddProduct("TSH-RED-M", "T-Shirt Sport", "Rouge", "M", 50);
        carton1A.AddProduct("SHO-BLK-42", "Baskets Running", "Noir", "42", 10);
        carton1A.AddProduct("TSH-BLU-L", "T-Shirt Sport", "Bleu", "L", 40);
        var carton1B = pallet1.AddCarton("CART-01-B");
        carton1B.AddProduct("SOC-WHT-U", "Chaussettes Sport", "Blanc", "U", 120);
        carton1B.AddProduct("SOC-BLK-U", "Chaussettes Sport", "Noir", "U", 80);

        var pallet2 = delivery.AddPallet("PAL-02");
        var carton2A = pallet2.AddCarton("CART-02-A");
        carton2A.AddProduct("SHO-WHT-40", "Baskets Running", "Blanc", "40", 12);
        carton2A.AddProduct("SHO-WHT-41", "Baskets Running", "Blanc", "41", 12);
        carton2A.AddProduct("SHO-BLK-43", "Baskets Running", "Noir", "43", 8);
        var carton2B = pallet2.AddCarton("CART-02-B");
        carton2B.AddProduct("SHT-BLK-M", "Short Training", "Noir", "M", 30);
        carton2B.AddProduct("SHT-GRY-L", "Short Training", "Gris", "L", 30);
        var carton2C = pallet2.AddCarton("CART-02-C");
        carton2C.AddProduct("BAL-FTB-05", "Ballon Football", "Blanc", "T5", 24);

        var pallet3 = delivery.AddPallet("PAL-03");
        var carton3A = pallet3.AddCarton("CART-03-A");
        carton3A.AddProduct("RKT-TEN-01", "Raquette Tennis", "Noir", "U", 6);
        carton3A.AddProduct("RKT-BAD-02", "Raquette Badminton", "Rouge", "U", 10);
        var carton3B = pallet3.AddCarton("CART-03-B");
        carton3B.AddProduct("BOT-STL-75", "Gourde Inox", "Argent", "75cl", 36);
        carton3B.AddProduct("BOT-STL-50", "Gourde Inox", "Argent", "50cl", 36);

        return delivery;
    }
}

import type { Carton, Delivery, ProductLine, ValidationStatus } from '@/features/reception/types';

export const deliveryId = 'delivery-1';
export const palletId = 'pallet-1';
export const cartonAId = 'carton-a';
export const cartonBId = 'carton-b';
export const productId = 'product-tsh';

interface DeliveryFixtureOptions {
    palletStatus?: ValidationStatus;
    cartonStatuses?: readonly [ValidationStatus, ValidationStatus];
    received?: number;
}

const product = (id: string, reference: string, name: string, color: string, size: string, expectedQuantity: number): ProductLine => ({
    id,
    reference,
    name,
    color,
    size,
    expectedQuantity,
    receivedQuantity: 0,
    status: 'none'
});

// The levels are deliberately not kept consistent with each other: screens must show what the payload says,
// so a test can set each level independently.
export function deliveryFixture({
    palletStatus = 'none',
    cartonStatuses = ['none', 'none'],
    received = 0
}: DeliveryFixtureOptions = {}): Delivery {
    const cartons: Carton[] = [
        {
            id: cartonAId,
            code: 'CART-01-A',
            status: cartonStatuses[0],
            progress: { receivedUnits: 0, expectedUnits: 60 },
            products: [
                product(productId, 'TSH-RED-M', 'T-shirt technique', 'Rouge', 'M', 50),
                product('product-sho', 'SHO-BLK-42', 'Chaussure de running', 'Noir', '42', 10)
            ]
        },
        {
            id: cartonBId,
            code: 'CART-01-B',
            status: cartonStatuses[1],
            progress: { receivedUnits: 0, expectedUnits: 120 },
            products: [product('product-soc', 'SOC-WHT-U', 'Chaussettes de sport', 'Blanc', 'U', 120)]
        }
    ];

    return {
        id: deliveryId,
        orderId: 'CMD-2026',
        status: 'none',
        progress: { receivedUnits: received, expectedUnits: 180 },
        pallets: [
            {
                id: palletId,
                code: 'PAL-01',
                status: palletStatus,
                progress: { receivedUnits: received, expectedUnits: 180 },
                cartons
            }
        ]
    };
}

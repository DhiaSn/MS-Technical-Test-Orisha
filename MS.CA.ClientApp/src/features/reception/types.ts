export type ValidationStatus = 'none' | 'partial' | 'all';

export interface Progress {
    receivedUnits: number;
    expectedUnits: number;
}

export interface ProductLine {
    id: string;
    reference: string;
    name: string;
    color: string;
    size: string;
    expectedQuantity: number;
    receivedQuantity: number;
    status: ValidationStatus;
}

export interface Carton {
    id: string;
    code: string;
    status: ValidationStatus;
    progress: Progress;
    products: ProductLine[];
}

export interface Pallet {
    id: string;
    code: string;
    status: ValidationStatus;
    progress: Progress;
    cartons: Carton[];
}

export interface Delivery {
    id: string;
    orderId: string;
    status: ValidationStatus;
    progress: Progress;
    pallets: Pallet[];
}

import type { Delivery } from '@/features/reception/types';
import { httpClient } from './httpClient';

const deliveries = '/reception/deliveries';
const id = encodeURIComponent;

export const receptionService = {
    getCurrent(signal?: AbortSignal): Promise<Delivery> {
        return httpClient.get<Delivery>(`${deliveries}/current`, { signal });
    },

    setPalletValidated(deliveryId: string, palletId: string, validated: boolean): Promise<Delivery> {
        return httpClient.put<Delivery>(`${deliveries}/${id(deliveryId)}/pallets/${id(palletId)}/validation`, { validated });
    },

    setCartonValidated(deliveryId: string, cartonId: string, validated: boolean): Promise<Delivery> {
        return httpClient.put<Delivery>(`${deliveries}/${id(deliveryId)}/cartons/${id(cartonId)}/validation`, { validated });
    },

    setProductValidated(deliveryId: string, productId: string, validated: boolean): Promise<Delivery> {
        return httpClient.put<Delivery>(`${deliveries}/${id(deliveryId)}/products/${id(productId)}/validation`, { validated });
    },

    setReceivedQuantity(deliveryId: string, productId: string, receivedQuantity: number): Promise<Delivery> {
        return httpClient.put<Delivery>(`${deliveries}/${id(deliveryId)}/products/${id(productId)}/received-quantity`, {
            receivedQuantity
        });
    }
} as const;

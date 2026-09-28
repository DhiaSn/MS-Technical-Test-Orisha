'use client';

import { useQuery } from '@tanstack/react-query';
import { receptionService } from '@/api';
import { deliveryKeys } from './deliveryKeys';

export function useDelivery() {
    return useQuery({
        queryKey: deliveryKeys.current,
        queryFn: ({ signal }) => receptionService.getCurrent(signal)
    });
}

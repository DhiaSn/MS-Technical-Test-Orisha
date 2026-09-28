import { renderHook } from '@testing-library/react';
import { deliveryKeys } from '@/features/reception/hooks/deliveryKeys';
import { useDelivery } from '@/features/reception/hooks/useDelivery';
import { useDeliveryMutations } from '@/features/reception/hooks/useDeliveryMutations';
import type { Delivery } from '@/features/reception/types';
import { createTestClient, TestProviders } from '@/shared/testing/renderWithProviders';

interface RenderMutationsOptions {
    seed?: Delivery;
    /** Mounts the delivery query, as the reception page does, so that a refetch has an observer to serve. */
    observe?: boolean;
}

export function renderMutations(deliveryId: string, { seed, observe = false }: RenderMutationsOptions = {}) {
    const client = createTestClient();
    if (seed !== undefined) client.setQueryData(deliveryKeys.current, seed);

    const wrapper = ({ children }: { children: React.ReactNode }) => <TestProviders client={client}>{children}</TestProviders>;
    if (observe) renderHook(() => useDelivery(), { wrapper });

    const view = renderHook(() => useDeliveryMutations(deliveryId), { wrapper });

    return { ...view, client };
}

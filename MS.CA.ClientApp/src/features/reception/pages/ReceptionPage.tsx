'use client';

import { normalizeError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import { ApiErrorState, EmptyState, Spinner } from '@/shared/components';
import { DeliveryHeader } from '@/features/reception/components/DeliveryHeader';
import { DeliveryTree } from '@/features/reception/components/DeliveryTree';
import { useDelivery } from '@/features/reception/hooks/useDelivery';
import { useDeliveryMutations } from '@/features/reception/hooks/useDeliveryMutations';
import type { Delivery } from '@/features/reception/types';

export function ReceptionPage() {
    const { data: delivery, error, isPending, refetch } = useDelivery();

    if (isPending) return <Spinner label={translate('reception.loading')} />;

    if (error !== null) {
        const appError = normalizeError(error);
        return appError.code === 'reception.delivery_not_found' ? (
            <EmptyState title={translate('reception.empty.title')} description={translate('reception.empty.description')} />
        ) : (
            <ApiErrorState title={translate('reception.error.title')} error={appError} onRetry={() => void refetch()} />
        );
    }

    return <DeliveryView delivery={delivery} />;
}

function DeliveryView({ delivery }: { delivery: Delivery }) {
    const mutations = useDeliveryMutations(delivery.id);

    return (
        <>
            <DeliveryHeader delivery={delivery} />
            <DeliveryTree
                delivery={delivery}
                isPending={mutations.isPending}
                onTogglePallet={(id, validated) => void mutations.togglePallet(id, validated)}
                onToggleCarton={(id, validated) => void mutations.toggleCarton(id, validated)}
                onToggleProduct={(id, validated) => void mutations.toggleProduct(id, validated)}
                onSetQuantity={mutations.setQuantity}
            />
        </>
    );
}

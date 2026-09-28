'use client';

import { useState } from 'react';
import { translate } from '@/core/i18n/translate';
import type { AppError } from '@/core/errors';
import type { Delivery } from '@/features/reception/types';
import { PalletNode } from './PalletNode';
import styles from './DeliveryTree.module.scss';

export interface DeliveryTreeProps {
    delivery: Delivery;
    isPending: (nodeId: string) => boolean;
    onTogglePallet: (palletId: string, validated: boolean) => void;
    onToggleCarton: (cartonId: string, validated: boolean) => void;
    onToggleProduct: (productId: string, validated: boolean) => void;
    onSetQuantity: (productId: string, quantity: number) => Promise<AppError | undefined>;
}

export function DeliveryTree({ delivery, isPending, onTogglePallet, onToggleCarton, onToggleProduct, onSetQuantity }: DeliveryTreeProps) {
    const [expandedIds, setExpandedIds] = useState<ReadonlySet<string>>(new Set());

    const toggleExpanded = (nodeId: string) =>
        setExpandedIds((previous) => {
            const next = new Set(previous);
            if (next.has(nodeId)) next.delete(nodeId);
            else next.add(nodeId);
            return next;
        });

    return (
        <ul className={styles.tree} aria-label={translate('tree.label')}>
            {delivery.pallets.map((pallet) => (
                <PalletNode
                    key={pallet.id}
                    pallet={pallet}
                    expandedIds={expandedIds}
                    onToggleExpanded={toggleExpanded}
                    isPending={isPending}
                    onTogglePallet={onTogglePallet}
                    onToggleCarton={onToggleCarton}
                    onToggleProduct={onToggleProduct}
                    onSetQuantity={onSetQuantity}
                />
            ))}
        </ul>
    );
}

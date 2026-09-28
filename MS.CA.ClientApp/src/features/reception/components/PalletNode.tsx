import { useId } from 'react';
import { translate } from '@/core/i18n/translate';
import { TriStateCheckbox } from '@/shared/components';
import type { AppError } from '@/core/errors';
import type { Pallet } from '@/features/reception/types';
import { CartonNode } from './CartonNode';
import { StatusBadge } from './StatusBadge';
import styles from './PalletNode.module.scss';

export interface PalletNodeProps {
    pallet: Pallet;
    expandedIds: ReadonlySet<string>;
    onToggleExpanded: (nodeId: string) => void;
    isPending: (nodeId: string) => boolean;
    onTogglePallet: (palletId: string, validated: boolean) => void;
    onToggleCarton: (cartonId: string, validated: boolean) => void;
    onToggleProduct: (productId: string, validated: boolean) => void;
    onSetQuantity: (productId: string, quantity: number) => Promise<AppError | undefined>;
}

export function PalletNode({
    pallet,
    expandedIds,
    onToggleExpanded,
    isPending,
    onTogglePallet,
    onToggleCarton,
    onToggleProduct,
    onSetQuantity
}: PalletNodeProps) {
    const childrenId = useId();
    const open = expandedIds.has(pallet.id);
    const title = translate('pallet.title', { code: pallet.code });

    return (
        <li className={styles.node}>
            <div className={styles.row}>
                <button
                    type="button"
                    className={styles.disclosure}
                    aria-expanded={open}
                    aria-controls={childrenId}
                    aria-label={translate(open ? 'node.collapse' : 'node.expand', { name: title })}
                    onClick={() => onToggleExpanded(pallet.id)}
                >
                    <span aria-hidden="true">{open ? '▾' : '▸'}</span>
                </button>
                <TriStateCheckbox
                    status={pallet.status}
                    label={translate('pallet.validate', { code: pallet.code })}
                    busy={isPending(pallet.id)}
                    onChange={(validated) => onTogglePallet(pallet.id, validated)}
                />
                <span className={styles.title}>{title}</span>
                <StatusBadge status={pallet.status} />
                <span className={styles.progress}>
                    {translate('node.progress', { received: pallet.progress.receivedUnits, expected: pallet.progress.expectedUnits })}
                </span>
            </div>
            {open && (
                <ul id={childrenId} className={styles.children}>
                    {pallet.cartons.map((carton) => (
                        <CartonNode
                            key={carton.id}
                            carton={carton}
                            open={expandedIds.has(carton.id)}
                            onToggleOpen={() => onToggleExpanded(carton.id)}
                            isPending={isPending}
                            onToggleCarton={onToggleCarton}
                            onToggleProduct={onToggleProduct}
                            onSetQuantity={onSetQuantity}
                        />
                    ))}
                </ul>
            )}
        </li>
    );
}

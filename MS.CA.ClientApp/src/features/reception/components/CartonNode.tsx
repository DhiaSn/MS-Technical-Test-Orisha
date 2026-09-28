import { useId } from 'react';
import { translate } from '@/core/i18n/translate';
import { TriStateCheckbox } from '@/shared/components';
import type { AppError } from '@/core/errors';
import type { Carton } from '@/features/reception/types';
import { ProductRow } from './ProductRow';
import { StatusBadge } from './StatusBadge';
import styles from './CartonNode.module.scss';

export interface CartonNodeProps {
    carton: Carton;
    open: boolean;
    onToggleOpen: () => void;
    isPending: (nodeId: string) => boolean;
    onToggleCarton: (cartonId: string, validated: boolean) => void;
    onToggleProduct: (productId: string, validated: boolean) => void;
    onSetQuantity: (productId: string, quantity: number) => Promise<AppError | undefined>;
}

export function CartonNode({ carton, open, onToggleOpen, isPending, onToggleCarton, onToggleProduct, onSetQuantity }: CartonNodeProps) {
    const childrenId = useId();
    const title = translate('carton.title', { code: carton.code });

    return (
        <li className={styles.node}>
            <div className={styles.row}>
                <button
                    type="button"
                    className={styles.disclosure}
                    aria-expanded={open}
                    aria-controls={childrenId}
                    aria-label={translate(open ? 'node.collapse' : 'node.expand', { name: title })}
                    onClick={onToggleOpen}
                >
                    <span aria-hidden="true">{open ? '▾' : '▸'}</span>
                </button>
                <TriStateCheckbox
                    status={carton.status}
                    label={translate('carton.validate', { code: carton.code })}
                    busy={isPending(carton.id)}
                    onChange={(validated) => onToggleCarton(carton.id, validated)}
                />
                <span className={styles.title}>{title}</span>
                <StatusBadge status={carton.status} />
                <span className={styles.progress}>
                    {translate('node.progress', { received: carton.progress.receivedUnits, expected: carton.progress.expectedUnits })}
                </span>
            </div>
            {open && (
                <ul id={childrenId} className={styles.children}>
                    {carton.products.map((product) => (
                        <ProductRow
                            key={product.id}
                            product={product}
                            isPending={isPending(product.id)}
                            onToggle={(validated) => onToggleProduct(product.id, validated)}
                            onSetQuantity={(quantity) => onSetQuantity(product.id, quantity)}
                        />
                    ))}
                </ul>
            )}
        </li>
    );
}

import { translate } from '@/core/i18n/translate';
import { TriStateCheckbox } from '@/shared/components';
import type { AppError } from '@/core/errors';
import type { ProductLine } from '@/features/reception/types';
import { ReceivedQuantityInput } from './ReceivedQuantityInput';
import { StatusBadge } from './StatusBadge';
import styles from './ProductRow.module.scss';

export interface ProductRowProps {
    product: ProductLine;
    isPending: boolean;
    onToggle: (validated: boolean) => void;
    onSetQuantity: (quantity: number) => Promise<AppError | undefined>;
}

export function ProductRow({ product, isPending, onToggle, onSetQuantity }: ProductRowProps) {
    return (
        <li className={styles.row}>
            <TriStateCheckbox
                status={product.status}
                label={translate('product.validate', { reference: product.reference })}
                busy={isPending}
                onChange={onToggle}
            />
            <div className={styles.identity}>
                <span className={styles.reference}>{product.reference}</span>
                <span className={styles.name}>{product.name}</span>
                <span className={styles.details}>{translate('product.details', { color: product.color, size: product.size })}</span>
            </div>
            <StatusBadge status={product.status} />
            <ReceivedQuantityInput
                reference={product.reference}
                expected={product.expectedQuantity}
                received={product.receivedQuantity}
                disabled={isPending}
                onCommit={onSetQuantity}
            />
        </li>
    );
}

import { statusLabel } from '@/features/reception/format';
import type { ValidationStatus } from '@/features/reception/types';
import styles from './StatusBadge.module.scss';

export interface StatusBadgeProps {
    status: ValidationStatus;
}

const ICONS = { none: '○', partial: '◐', all: '●' } as const satisfies Record<ValidationStatus, string>;

export function StatusBadge({ status }: StatusBadgeProps) {
    return (
        <span className={styles.badge} data-status={status}>
            <span className={styles.icon} aria-hidden="true">
                {ICONS[status]}
            </span>
            {statusLabel(status)}
        </span>
    );
}

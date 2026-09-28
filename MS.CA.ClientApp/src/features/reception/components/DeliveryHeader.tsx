import { translate } from '@/core/i18n/translate';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import type { Delivery } from '@/features/reception/types';
import { ProgressGauge } from './ProgressGauge';
import { StatusBadge } from './StatusBadge';
import styles from './DeliveryHeader.module.scss';

export interface DeliveryHeaderProps {
    delivery: Delivery;
}

export function DeliveryHeader({ delivery }: DeliveryHeaderProps) {
    const title = translate('reception.title', { orderId: delivery.orderId });
    usePageTitle(title);

    return (
        <header className={styles.header}>
            <div className={styles.top}>
                <h1 className={styles.title}>{title}</h1>
                <StatusBadge status={delivery.status} />
            </div>
            <ProgressGauge progress={delivery.progress} status={delivery.status} />
        </header>
    );
}

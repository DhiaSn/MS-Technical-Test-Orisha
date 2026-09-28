import { translate } from '@/core/i18n/translate';
import { ProgressBar } from '@/shared/components';
import type { Progress, ValidationStatus } from '@/features/reception/types';
import styles from './ProgressGauge.module.scss';

export interface ProgressGaugeProps {
    progress: Progress;
    status: ValidationStatus;
}

export function ProgressGauge({ progress, status }: ProgressGaugeProps) {
    return (
        <div className={styles.gauge}>
            <p className={styles.text}>
                {translate('reception.progress', { received: progress.receivedUnits, expected: progress.expectedUnits })}
            </p>
            <ProgressBar value={progress.receivedUnits} max={progress.expectedUnits} label={translate('reception.progress.label')} />
            {status === 'all' && <p className={styles.done}>{translate('reception.complete')}</p>}
        </div>
    );
}

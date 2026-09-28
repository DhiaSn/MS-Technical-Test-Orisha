import { translate } from '@/core/i18n/translate';
import { cx } from './cx';
import styles from './Alert.module.scss';

export type AlertTone = 'error' | 'info';

export interface AlertProps {
    tone: AlertTone;
    children: React.ReactNode;
    className?: string;
}

const ICONS = { error: '⚠', info: 'ℹ' } as const;
const TONE_LABELS = { error: 'tone.error', info: 'tone.info' } as const;

export function Alert({ tone, children, className }: AlertProps) {
    return (
        <div className={cx(styles.alert, styles[tone], className)} role={tone === 'error' ? 'alert' : 'status'}>
            <span className={styles.icon} aria-hidden="true">
                {ICONS[tone]}
            </span>
            <span className="visually-hidden">{`${translate(TONE_LABELS[tone])} : `}</span>
            <span>{children}</span>
        </div>
    );
}

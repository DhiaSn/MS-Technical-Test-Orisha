import styles from './ProgressBar.module.scss';

export interface ProgressBarProps {
    value: number;
    max: number;
    label: string;
}

export function ProgressBar({ value, max, label }: ProgressBarProps) {
    const percent = max <= 0 ? 0 : Math.round((value / max) * 100);

    return (
        <div className={styles.track} role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={max} aria-valuenow={value}>
            <div className={styles.fill} data-testid="progress-fill" style={{ width: `${percent}%` }} />
        </div>
    );
}

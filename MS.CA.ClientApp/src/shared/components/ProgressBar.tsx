import styles from './ProgressBar.module.scss';

export interface ProgressBarProps {
    value: number;
    max: number;
    label: string;
}

export function ProgressBar({ value, max, label }: ProgressBarProps) {
    const percent = max <= 0 ? 0 : Math.round((value / max) * 100);
    const isComplete = max > 0 && value === max;

    return (
        <div className={styles.track} role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={max} aria-valuenow={value}>
            <div className={styles.fill} data-testid="progress-fill" data-complete={isComplete} style={{ width: `${percent}%` }} />
        </div>
    );
}

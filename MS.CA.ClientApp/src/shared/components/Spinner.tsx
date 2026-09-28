import styles from './Spinner.module.scss';

export interface SpinnerProps {
    label: string;
}

export function Spinner({ label }: SpinnerProps) {
    return (
        <div className={styles.spinner} role="status">
            <span className={styles.ring} aria-hidden="true" />
            <span className={styles.label}>{label}</span>
        </div>
    );
}

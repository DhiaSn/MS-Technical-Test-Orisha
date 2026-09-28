import styles from './Spinner.module.scss';

export interface SpinnerProps {
    label: string;
}

export function Spinner({ label }: SpinnerProps) {
    return (
        <span className={styles.spinner} role="status">
            <span className={styles.ring} aria-hidden="true" />
            <span className="visually-hidden">{label}</span>
        </span>
    );
}

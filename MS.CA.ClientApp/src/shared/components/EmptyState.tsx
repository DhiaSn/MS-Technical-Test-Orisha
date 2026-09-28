import styles from './EmptyState.module.scss';

export interface EmptyStateProps {
    title: string;
    description?: string;
    action?: React.ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
    return (
        <div className={styles.state}>
            <h2 className={styles.title}>{title}</h2>
            {description !== undefined && <p className={styles.description}>{description}</p>}
            {action}
        </div>
    );
}

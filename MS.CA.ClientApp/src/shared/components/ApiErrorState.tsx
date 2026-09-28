import { describeError, type AppError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import { Button } from './Button';
import styles from './ApiErrorState.module.scss';

export interface ApiErrorStateProps {
    title?: string;
    error: AppError;
    onRetry: () => void;
}

export function ApiErrorState({ title = translate('error.loadFailed'), error, onRetry }: ApiErrorStateProps) {
    return (
        <div className={styles.state} role="alert">
            <h2 className={styles.title}>{title}</h2>
            <p className={styles.message}>{describeError(error)}</p>
            <Button variant="secondary" onClick={onRetry}>
                {translate('action.retry')}
            </Button>
        </div>
    );
}

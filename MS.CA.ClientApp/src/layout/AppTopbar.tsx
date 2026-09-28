import { translate } from '@/core/i18n/translate';
import styles from './AppTopbar.module.scss';

export function AppTopbar() {
    return (
        <header className={styles.topbar}>
            <span className={styles.title}>{translate('app.title')}</span>
        </header>
    );
}

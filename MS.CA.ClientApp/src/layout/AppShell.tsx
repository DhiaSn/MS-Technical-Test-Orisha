import { translate } from '@/core/i18n/translate';
import { AppTopbar } from './AppTopbar';
import styles from './AppShell.module.scss';

export function AppShell({ children }: { children: React.ReactNode }) {
    return (
        <div className={styles.shell}>
            <a href="#main" className={styles.skipLink}>
                {translate('skip.toContent')}
            </a>
            <AppTopbar />
            <main id="main" className={styles.content}>
                {children}
            </main>
        </div>
    );
}

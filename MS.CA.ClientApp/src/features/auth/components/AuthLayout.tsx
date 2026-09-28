import { translate } from '@/core/i18n/translate';
import styles from './AuthLayout.module.scss';

export interface AuthLayoutProps {
    title: string;
    subtitle?: string;
    children: React.ReactNode;
}

export function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
    return (
        <main className={styles.page}>
            <div className={styles.card}>
                <p className={styles.brand}>{translate('app.title')}</p>
                <h1 className={styles.title}>{title}</h1>
                {subtitle !== undefined && <p className={styles.subtitle}>{subtitle}</p>}
                {children}
            </div>
        </main>
    );
}

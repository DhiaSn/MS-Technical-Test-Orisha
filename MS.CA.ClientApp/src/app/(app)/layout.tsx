import { Suspense } from 'react';
import { translate } from '@/core/i18n/translate';
import { AuthGuard } from '@/features/auth/components/AuthGuard';
import { AppShell } from '@/layout';
import { Spinner } from '@/shared/components';

export default function AppGroupLayout({ children }: { children: React.ReactNode }) {
    return (
        <Suspense fallback={<Spinner label={translate('auth.loading')} />}>
            <AuthGuard>
                <AppShell>{children}</AppShell>
            </AuthGuard>
        </Suspense>
    );
}

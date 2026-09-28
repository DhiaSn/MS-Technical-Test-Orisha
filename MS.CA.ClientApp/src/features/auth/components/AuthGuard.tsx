'use client';

import { useEffect } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { routes } from '@/config/routes';
import { translate } from '@/core/i18n/translate';
import { useSession } from '@/features/auth/hooks/useSession';
import { ApiErrorState, Spinner } from '@/shared/components';

export function AuthGuard({ children }: { children: React.ReactNode }) {
    const { state, retry } = useSession();
    const router = useRouter();
    const pathname = usePathname();
    const search = useSearchParams().toString();

    useEffect(() => {
        if (state.status === 'signedOut') {
            const here = search === '' ? pathname : `${pathname}?${search}`;
            router.replace(`${routes.login}?returnTo=${encodeURIComponent(here)}`);
        }
    }, [state.status, pathname, search, router]);

    switch (state.status) {
        case 'signedIn':
            return <>{children}</>;
        case 'error':
            return <ApiErrorState title={translate('auth.session.error.title')} error={state.error} onRetry={retry} />;
        default:
            return <Spinner label={translate('auth.loading')} />;
    }
}

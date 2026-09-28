'use client';

import { useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { translate } from '@/core/i18n/translate';
import { AuthLayout } from '@/features/auth/components/AuthLayout';
import { LoginForm } from '@/features/auth/components/LoginForm';
import { useSession } from '@/features/auth/hooks/useSession';
import { safeReturnTo } from '@/features/auth/returnTo';
import { Spinner } from '@/shared/components';
import { usePageTitle } from '@/shared/hooks/usePageTitle';

export function LoginPage() {
    const { state, signIn } = useSession();
    const router = useRouter();
    const returnTo = safeReturnTo(useSearchParams().get('returnTo'));
    usePageTitle(translate('auth.login.title'));

    useEffect(() => {
        if (state.status === 'signedIn') router.replace(returnTo);
    }, [state.status, returnTo, router]);

    if (state.status === 'signedIn') return null;
    if (state.status === 'loading') return <Spinner label={translate('auth.loading')} />;

    return (
        <AuthLayout title={translate('auth.login.title')} subtitle={translate('auth.login.subtitle')}>
            <LoginForm
                onSubmit={async (credentials) => {
                    await signIn(credentials);
                }}
            />
        </AuthLayout>
    );
}

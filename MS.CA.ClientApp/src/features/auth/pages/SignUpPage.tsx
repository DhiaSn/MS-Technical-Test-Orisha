'use client';

import { useEffect } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { routes } from '@/config/routes';
import { translate } from '@/core/i18n/translate';
import { AuthLayout } from '@/features/auth/components/AuthLayout';
import { SignUpForm } from '@/features/auth/components/SignUpForm';
import { useSession } from '@/features/auth/hooks/useSession';
import { safeReturnTo } from '@/features/auth/returnTo';
import { Spinner } from '@/shared/components';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import styles from './SignUpPage.module.scss';

export function SignUpPage() {
    const { state, register } = useSession();
    const router = useRouter();
    const returnTo = safeReturnTo(useSearchParams().get('returnTo'));
    usePageTitle(translate('auth.signUp.title'));

    useEffect(() => {
        if (state.status === 'signedIn') router.replace(returnTo);
    }, [state.status, returnTo, router]);

    if (state.status === 'signedIn') return null;
    if (state.status === 'loading') return <Spinner label={translate('auth.loading')} />;

    return (
        <AuthLayout title={translate('auth.signUp.title')} subtitle={translate('auth.signUp.subtitle')}>
            <SignUpForm
                onSubmit={async (registration) => {
                    await register(registration);
                }}
            />
            <Link className={styles.link} href={routes.login}>
                {translate('auth.login.link')}
            </Link>
        </AuthLayout>
    );
}

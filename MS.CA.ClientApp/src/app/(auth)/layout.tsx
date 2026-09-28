import { Suspense } from 'react';
import { translate } from '@/core/i18n/translate';
import { Spinner } from '@/shared/components';

export default function AuthGroupLayout({ children }: { children: React.ReactNode }) {
    return <Suspense fallback={<Spinner label={translate('auth.loading')} />}>{children}</Suspense>;
}

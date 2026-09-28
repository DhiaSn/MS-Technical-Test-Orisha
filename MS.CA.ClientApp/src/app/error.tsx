'use client';

import { translate } from '@/core/i18n/translate';
import { Button, EmptyState } from '@/shared/components';

export default function ErrorPage({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
    return (
        <main>
            <EmptyState
                title={translate('error.page.title')}
                description={translate('error.unknown')}
                action={<Button onClick={reset}>{translate('action.retry')}</Button>}
            />
        </main>
    );
}

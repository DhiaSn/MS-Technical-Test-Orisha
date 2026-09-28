'use client';

import { useState } from 'react';
import { translate } from '@/core/i18n/translate';
import { useSession } from '@/features/auth/hooks/useSession';
import { Button, useToast } from '@/shared/components';
import styles from './AppTopbar.module.scss';

export function AppTopbar() {
    const { state, signOut } = useSession();
    const toast = useToast();
    const [busy, setBusy] = useState(false);

    // On success the provider flips to signed out and the guard unmounts this bar, so `busy` is left set.
    const onSignOut = async () => {
        setBusy(true);
        try {
            await signOut();
        } catch {
            toast.error(translate('auth.signOut.failed'));
            setBusy(false);
        }
    };

    return (
        <header className={styles.topbar}>
            <span className={styles.title}>{translate('app.title')}</span>
            {state.status === 'signedIn' && (
                <div className={styles.session}>
                    <span>{translate('auth.signedInAs', { name: state.session.displayName })}</span>
                    <Button variant="secondary" onClick={onSignOut} disabled={busy}>
                        {translate('auth.signOut')}
                    </Button>
                </div>
            )}
        </header>
    );
}

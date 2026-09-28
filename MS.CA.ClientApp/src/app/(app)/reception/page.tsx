import { translate } from '@/core/i18n/translate';
import { AppShell } from '@/layout';

export default function ReceptionRoute() {
    return (
        <AppShell>
            <h1>{translate('app.title')}</h1>
        </AppShell>
    );
}

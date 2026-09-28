import Link from 'next/link';
import { routes } from '@/config/routes';
import { translate } from '@/core/i18n/translate';
import { EmptyState } from '@/shared/components';

export default function NotFound() {
    return (
        <main>
            <EmptyState
                title={translate('notFound.title')}
                description={translate('notFound.description')}
                action={<Link href={routes.reception}>{translate('notFound.back')}</Link>}
            />
        </main>
    );
}

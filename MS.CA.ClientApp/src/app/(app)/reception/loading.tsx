import { translate } from '@/core/i18n/translate';
import { Spinner } from '@/shared/components';

export default function Loading() {
    return <Spinner label={translate('reception.loading')} />;
}

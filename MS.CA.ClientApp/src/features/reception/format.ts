import { translate } from '@/core/i18n/translate';
import type { Progress, ValidationStatus } from './types';

export function formatProgress({ receivedUnits, expectedUnits }: Progress): string {
    return `${receivedUnits} / ${expectedUnits}`;
}

export function percentOf({ receivedUnits, expectedUnits }: Progress): number {
    return expectedUnits <= 0 ? 0 : Math.round((receivedUnits / expectedUnits) * 100);
}

export function statusLabel(status: ValidationStatus): string {
    return translate(`status.${status}`);
}

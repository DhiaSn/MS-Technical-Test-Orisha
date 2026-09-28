import { errorCodesFr } from '@/core/i18n/messages/errorCodes.fr';
import { interpolate } from '@/core/i18n/translate';
import type { AppError } from './AppError';

// The code comes from the server: an inherited property name such as "constructor" must not resolve.
function templateFor(code: string): string | undefined {
    return Object.hasOwn(errorCodesFr, code) ? errorCodesFr[code] : undefined;
}

export function describeFieldErrors(error: AppError, field: string): string[] {
    const codes = error.fieldCodes?.[field] ?? [];

    return codes.flatMap((fieldError) => {
        const template = templateFor(fieldError.code);
        return template === undefined ? [] : [interpolate(template, fieldError.params)];
    });
}

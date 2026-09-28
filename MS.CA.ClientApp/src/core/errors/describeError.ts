import { errorCodesFr } from '@/core/i18n/messages/errorCodes.fr';
import { interpolate, translate } from '@/core/i18n/translate';
import type { AppError } from './AppError';

const KIND_MESSAGE = {
    network: 'error.network',
    timeout: 'error.timeout',
    unauthorized: 'error.unauthorized',
    notFound: 'error.notFound',
    validation: 'error.validation',
    server: 'error.server',
    unknown: 'error.unknown'
} as const;

// The code comes from the server: an inherited property name such as "constructor" must not resolve.
function templateFor(code: string | undefined): string | undefined {
    return code !== undefined && Object.hasOwn(errorCodesFr, code) ? errorCodesFr[code] : undefined;
}

export function describeError(error: AppError, field?: string): string {
    const fieldError = field === undefined ? undefined : error.fieldCodes?.[field]?.[0];
    const template = templateFor(fieldError?.code ?? error.code);

    return template === undefined ? translate(KIND_MESSAGE[error.kind]) : interpolate(template, fieldError?.params);
}

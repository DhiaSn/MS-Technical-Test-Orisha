import { AppError, isAppError } from './AppError';

export function normalizeError(cause: unknown): AppError {
    if (isAppError(cause)) return cause;

    if (cause instanceof DOMException && (cause.name === 'TimeoutError' || cause.name === 'AbortError')) {
        return new AppError({ kind: 'timeout', message: cause.message, cause });
    }

    if (cause instanceof TypeError) {
        return new AppError({ kind: 'network', message: cause.message, cause });
    }

    return new AppError({ kind: 'unknown', message: String(cause), cause });
}

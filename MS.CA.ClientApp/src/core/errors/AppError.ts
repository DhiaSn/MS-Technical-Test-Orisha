export type AppErrorKind = 'network' | 'timeout' | 'unauthorized' | 'notFound' | 'validation' | 'server' | 'unknown';

/** One reason a field was rejected: a stable code and the limits its message may quote. */
export interface FieldErrorCode {
    code: string;
    params?: Readonly<Record<string, string>>;
}

export interface AppErrorOptions {
    kind: AppErrorKind;
    /** Diagnostic text for logs. It is never shown to a user: screens localise from `code`. */
    message: string;
    status?: number;
    /** The backend's stable machine-readable code. */
    code?: string;
    fieldCodes?: Readonly<Record<string, readonly FieldErrorCode[]>>;
    cause?: unknown;
}

export class AppError extends Error {
    readonly kind: AppErrorKind;
    readonly status?: number;
    readonly code?: string;
    readonly fieldCodes?: Readonly<Record<string, readonly FieldErrorCode[]>>;

    constructor({ kind, message, status, code, fieldCodes, cause }: AppErrorOptions) {
        super(message, { cause });
        this.name = 'AppError';
        this.kind = kind;
        this.status = status;
        this.code = code;
        this.fieldCodes = fieldCodes;
    }
}

export function isAppError(value: unknown): value is AppError {
    return value instanceof AppError;
}

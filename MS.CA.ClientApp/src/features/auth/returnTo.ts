import { routes } from '@/config/routes';

const AUTH_PATHS: readonly string[] = [routes.login, routes.signUp];

// A local path only: one leading slash, no scheme, no protocol-relative form, no backslashes or
// control characters (browsers normalise "/\host" to "//host"). The auth pages are excluded so a
// sign-in cannot redirect back into itself.
export function safeReturnTo(value: string | null | undefined, fallback: string = routes.reception): string {
    if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) return fallback;
    if (/[\\u0000-\u001f\u007f]/.test(value)) return fallback;

    const path = value.split(/[?#]/, 1)[0] ?? '';
    return AUTH_PATHS.includes(path) ? fallback : value;
}

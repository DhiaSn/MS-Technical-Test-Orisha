import { routes } from '@/config/routes';

const AUTH_PATHS: readonly string[] = [routes.login, routes.signUp];

const LAST_CONTROL_CHARACTER = 0x1f;
const DELETE_CHARACTER = 0x7f;

function hasUnsafeCharacter(value: string): boolean {
    return [...value].some((char) => {
        const code = char.charCodeAt(0);
        return char === '\\' || code <= LAST_CONTROL_CHARACTER || code === DELETE_CHARACTER;
    });
}

// A local path only: one leading slash, no scheme, no protocol-relative form, no backslashes or
// control characters (browsers normalise "/\host" to "//host"). The auth pages are excluded so a
// sign-in cannot redirect back into itself.
export function safeReturnTo(value: string | null | undefined, fallback: string = routes.reception): string {
    if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) return fallback;
    if (hasUnsafeCharacter(value)) return fallback;

    // Next serves these routes regardless of case and trailing slashes, so the comparison must too.
    const path = (value.split(/[?#]/, 1)[0] ?? '').replace(/\/+$/, '').toLowerCase();
    return AUTH_PATHS.includes(path) ? fallback : value;
}

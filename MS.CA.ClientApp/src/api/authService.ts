import { httpClient } from '@/api/httpClient';
import type { Credentials, Session } from '@/features/auth/types';

// For these calls a 401 is the answer, not an expiry, so it is not reported to the session store.
const AUTH_CALL = { sessionAware: false } as const;

export const authService = {
    signIn(credentials: Credentials, signal?: AbortSignal): Promise<Session> {
        return httpClient.post<Session>('/identity/auth/sign-in', credentials, { signal, ...AUTH_CALL });
    },

    signOut(signal?: AbortSignal): Promise<void> {
        return httpClient.post<void>('/identity/auth/sign-out', undefined, { signal, ...AUTH_CALL });
    },

    currentSession(signal?: AbortSignal): Promise<Session> {
        return httpClient.get<Session>('/identity/auth/me', { signal, ...AUTH_CALL });
    }
} as const;

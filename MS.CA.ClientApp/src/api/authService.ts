import { httpClient } from '@/api/httpClient';
import type { Credentials, PasswordPolicy, Registration, Session } from '@/features/auth/types';

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
    },

    register(registration: Registration, signal?: AbortSignal): Promise<Session> {
        return httpClient.post<Session>('/identity/auth/register', registration, { signal, ...AUTH_CALL });
    },

    passwordPolicy(signal?: AbortSignal): Promise<PasswordPolicy> {
        return httpClient.get<PasswordPolicy>('/identity/auth/password-policy', { signal, ...AUTH_CALL });
    }
} as const;

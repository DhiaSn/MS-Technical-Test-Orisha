'use client';

import { createContext, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authService, sessionEvents } from '@/api';
import { type AppError, normalizeError } from '@/core/errors';
import type { Credentials, Session } from '@/features/auth/types';

export type SessionState =
    { status: 'loading' } | { status: 'signedOut' } | { status: 'signedIn'; session: Session } | { status: 'error'; error: AppError };

export interface SessionValue {
    state: SessionState;
    signIn: (credentials: Credentials) => Promise<Session>;
    signOut: () => Promise<void>;
    retry: () => void;
}

export const SessionContext = createContext<SessionValue | undefined>(undefined);

export function SessionProvider({ children }: { children: React.ReactNode }) {
    const client = useQueryClient();
    const [state, setState] = useState<SessionState>({ status: 'loading' });
    const [attempt, setAttempt] = useState(0);
    const restore = useRef<AbortController>(null);

    // The cookie is HttpOnly, so only the server can say whether there is a session.
    useEffect(() => {
        const controller = new AbortController();
        restore.current = controller;

        authService
            .currentSession(controller.signal)
            .then((session) => {
                if (!controller.signal.aborted) setState({ status: 'signedIn', session });
            })
            .catch((cause: unknown) => {
                if (controller.signal.aborted) return;
                const error = normalizeError(cause);
                // Only a 401 means "no session"; anything else says nothing about it.
                setState(error.kind === 'unauthorized' ? { status: 'signedOut' } : { status: 'error', error });
            });

        return () => controller.abort();
    }, [attempt]);

    // Whatever the restore answers after this point predates the change and must not overwrite it.
    const settle = useCallback(
        (next: SessionState) => {
            restore.current?.abort();
            client.clear();
            setState(next);
        },
        [client]
    );

    const endSession = useCallback(() => settle({ status: 'signedOut' }), [settle]);

    useEffect(() => sessionEvents.onEnded(endSession), [endSession]);

    const signIn = useCallback(
        async (credentials: Credentials) => {
            const session = await authService.signIn(credentials);
            settle({ status: 'signedIn', session });
            return session;
        },
        [settle]
    );

    const signOut = useCallback(async () => {
        try {
            await authService.signOut();
        } catch (cause) {
            // An expired session is already signed out; only a failure to reach the server leaves it open.
            if (normalizeError(cause).kind !== 'unauthorized') throw cause;
        }
        endSession();
    }, [endSession]);

    const retry = useCallback(() => {
        setState({ status: 'loading' });
        setAttempt((n) => n + 1);
    }, []);

    const value = useMemo<SessionValue>(() => ({ state, signIn, signOut, retry }), [state, signIn, signOut, retry]);

    return <SessionContext value={value}>{children}</SessionContext>;
}

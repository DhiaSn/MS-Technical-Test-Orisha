import type { ReactNode } from 'react';
import { SessionContext, type SessionState, type SessionValue } from '@/features/auth/context/SessionContext';

export type SessionStub = SessionValue & { signIn: jest.Mock; signOut: jest.Mock; retry: jest.Mock };

export function createSessionStub(state: SessionState): SessionStub {
    return { state, signIn: jest.fn(), signOut: jest.fn(), retry: jest.fn() };
}

export function StubSessionProvider({ value, children }: { value: SessionStub; children: ReactNode }) {
    return <SessionContext value={value}>{children}</SessionContext>;
}

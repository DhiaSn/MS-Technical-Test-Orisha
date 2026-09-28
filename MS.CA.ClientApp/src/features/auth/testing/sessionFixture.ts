import type { Session } from '@/features/auth/types';

export function sessionFixture(overrides: Partial<Session> = {}): Session {
    return {
        userId: '5f0c5b57-3d1e-4a52-9d7a-0d4b5e2f6a10',
        username: 'magasinier',
        displayName: 'Magasinier',
        role: 'Operator',
        accessTokenExpiresAt: '2026-01-01T12:00:00Z',
        ...overrides
    };
}

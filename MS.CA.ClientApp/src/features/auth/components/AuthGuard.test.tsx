import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { authService, sessionEvents } from '@/api';
import { AppError } from '@/core/errors';
import { SessionProvider, type SessionState } from '@/features/auth/context/SessionContext';
import { mockLocation, navigation, resetNavigation } from '@/features/auth/testing/mockNavigation';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { createSessionStub, StubSessionProvider } from '@/features/auth/testing/stubSession';
import { renderWithProviders } from '@/shared/testing/renderWithProviders';
import { AuthGuard } from './AuthGuard';

jest.mock('next/navigation', () => jest.requireActual('@/features/auth/testing/mockNavigation').nextNavigationMock);
jest.mock('@/api/authService', () => ({
    authService: { currentSession: jest.fn(), signIn: jest.fn(), signOut: jest.fn() }
}));

function guarded() {
    return (
        <AuthGuard>
            <p>protected</p>
        </AuthGuard>
    );
}

function renderGuard(state: SessionState) {
    const stub = createSessionStub(state);
    render(<StubSessionProvider value={stub}>{guarded()}</StubSessionProvider>);
    return stub;
}

describe('AuthGuard', () => {
    beforeEach(() => resetNavigation());

    it('renders a spinner and no content while the session is loading', () => {
        renderGuard({ status: 'loading' });

        expect(screen.getByText('Vérification de la session…')).toBeInTheDocument();
        expect(screen.queryByText('protected')).not.toBeInTheDocument();
        expect(navigation.replace).not.toHaveBeenCalled();
    });

    it('sends a signed-out visitor to the login page and remembers where they were', () => {
        mockLocation('/reception', 'tab=1');
        renderGuard({ status: 'signedOut' });

        expect(navigation.replace).toHaveBeenCalledWith('/login?returnTo=%2Freception%3Ftab%3D1');
        expect(screen.queryByText('protected')).not.toBeInTheDocument();
    });

    it('redirects once, not on every render', () => {
        const stub = createSessionStub({ status: 'signedOut' });
        const { rerender } = render(<StubSessionProvider value={stub}>{guarded()}</StubSessionProvider>);

        rerender(<StubSessionProvider value={stub}>{guarded()}</StubSessionProvider>);

        expect(navigation.replace).toHaveBeenCalledTimes(1);
    });

    it('renders the content for a signed-in operator', () => {
        renderGuard({ status: 'signedIn', session: sessionFixture() });

        expect(screen.getByText('protected')).toBeInTheDocument();
        expect(navigation.replace).not.toHaveBeenCalled();
    });

    it('offers a retry when the session cannot be checked, without redirecting', async () => {
        const { retry } = renderGuard({ status: 'error', error: new AppError({ kind: 'network', message: 'x' }) });

        expect(screen.getByText('Impossible de vérifier votre session')).toBeInTheDocument();
        expect(screen.queryByText('protected')).not.toBeInTheDocument();
        await userEvent.click(screen.getByRole('button', { name: 'Réessayer' }));

        expect(retry).toHaveBeenCalled();
        expect(navigation.replace).not.toHaveBeenCalled();
    });

    describe('with the real session provider', () => {
        beforeEach(() => jest.resetAllMocks());

        it('never shows the content before the server has confirmed the session', async () => {
            (authService.currentSession as jest.Mock).mockResolvedValue(sessionFixture());
            renderWithProviders(<SessionProvider>{guarded()}</SessionProvider>);

            expect(screen.queryByText('protected')).not.toBeInTheDocument();
            expect(await screen.findByText('protected')).toBeInTheDocument();
        });

        it('takes the operator to the login page, with a way back, when the session expires mid-use', async () => {
            (authService.currentSession as jest.Mock).mockResolvedValue(sessionFixture());
            mockLocation('/reception', 'tab=1');
            const { client } = renderWithProviders(<SessionProvider>{guarded()}</SessionProvider>);
            client.setQueryData(['delivery', 'current'], { id: 'd1' });
            await screen.findByText('protected');

            act(() => sessionEvents.emitEnded());

            await waitFor(() => expect(navigation.replace).toHaveBeenCalledWith('/login?returnTo=%2Freception%3Ftab%3D1'));
            expect(navigation.replace).toHaveBeenCalledTimes(1);
            expect(screen.queryByText('protected')).not.toBeInTheDocument();
            expect(client.getQueryData(['delivery', 'current'])).toBeUndefined();
        });

        it('does not treat a network failure on load as a sign-out', async () => {
            (authService.currentSession as jest.Mock).mockRejectedValue(new AppError({ kind: 'network', message: 'x' }));
            renderWithProviders(<SessionProvider>{guarded()}</SessionProvider>);

            expect(await screen.findByRole('button', { name: 'Réessayer' })).toBeInTheDocument();
            expect(navigation.replace).not.toHaveBeenCalled();
        });
    });
});

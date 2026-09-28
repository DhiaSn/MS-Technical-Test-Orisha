import { useState } from 'react';
import { act, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { authService, sessionEvents } from '@/api';
import { AppError } from '@/core/errors';
import { useSession } from '@/features/auth/hooks/useSession';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { renderWithProviders } from '@/shared/testing/renderWithProviders';
import { SessionProvider } from './SessionContext';

jest.mock('@/api/authService', () => ({
    authService: { currentSession: jest.fn(), signIn: jest.fn(), signOut: jest.fn() }
}));

const mocked = {
    currentSession: authService.currentSession as jest.Mock,
    signIn: authService.signIn as jest.Mock,
    signOut: authService.signOut as jest.Mock
};

const unauthorized = () => new AppError({ kind: 'unauthorized', message: 'x', status: 401 });
const networkFailure = () => new AppError({ kind: 'network', message: 'x' });

function Probe() {
    const { state, signIn, signOut, retry } = useSession();
    const [signInError, setSignInError] = useState<string>();
    const [signOutFailed, setSignOutFailed] = useState(false);

    return (
        <div>
            <span data-testid="status">{state.status}</span>
            {state.status === 'signedIn' && <span data-testid="name">{state.session.displayName}</span>}
            {signInError !== undefined && <span data-testid="signInError">{signInError}</span>}
            {signOutFailed && <span data-testid="signOutError">failed</span>}
            <button
                onClick={() =>
                    signIn({ username: 'magasinier', password: 'Reception2026' }).catch((error: AppError) => setSignInError(error.code))
                }
            >
                signIn
            </button>
            <button onClick={() => signOut().catch(() => setSignOutFailed(true))}>signOut</button>
            <button onClick={retry}>retry</button>
        </div>
    );
}

function renderProbe() {
    return renderWithProviders(
        <SessionProvider>
            <Probe />
        </SessionProvider>
    );
}

const status = () => screen.getByTestId('status');

describe('SessionProvider', () => {
    beforeEach(() => jest.resetAllMocks());

    it('restores the session from the server on load', async () => {
        mocked.currentSession.mockResolvedValue(sessionFixture());
        renderProbe();

        expect(status()).toHaveTextContent('loading');
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
        expect(screen.getByTestId('name')).toHaveTextContent('Magasinier');
    });

    it('is signed out when the server says there is no session', async () => {
        mocked.currentSession.mockRejectedValue(unauthorized());
        renderProbe();

        await waitFor(() => expect(status()).toHaveTextContent('signedOut'));
    });

    it('reports an error, not a sign-out, when the server cannot be reached', async () => {
        mocked.currentSession.mockRejectedValue(networkFailure());
        renderProbe();

        await waitFor(() => expect(status()).toHaveTextContent('error'));
    });

    it('does not treat a server failure as a sign-out either', async () => {
        mocked.currentSession.mockRejectedValue(new AppError({ kind: 'server', message: 'x', status: 500 }));
        renderProbe();

        await waitFor(() => expect(status()).toHaveTextContent('error'));
    });

    it('retries the restore on demand', async () => {
        mocked.currentSession.mockRejectedValueOnce(networkFailure()).mockResolvedValueOnce(sessionFixture());
        renderProbe();
        await waitFor(() => expect(status()).toHaveTextContent('error'));

        await userEvent.click(screen.getByRole('button', { name: 'retry' }));

        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
    });

    it('signs in and exposes the new session', async () => {
        mocked.currentSession.mockRejectedValue(unauthorized());
        mocked.signIn.mockResolvedValue(sessionFixture());
        renderProbe();
        await waitFor(() => expect(status()).toHaveTextContent('signedOut'));

        await userEvent.click(screen.getByRole('button', { name: 'signIn' }));

        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
        expect(authService.signIn).toHaveBeenCalledWith({ username: 'magasinier', password: 'Reception2026' });
    });

    it('leaves the state alone and rejects when sign-in fails', async () => {
        mocked.currentSession.mockRejectedValue(unauthorized());
        mocked.signIn.mockRejectedValue(new AppError({ kind: 'unauthorized', message: 'x', code: 'auth.invalid_credentials' }));
        renderProbe();
        await waitFor(() => expect(status()).toHaveTextContent('signedOut'));

        await userEvent.click(screen.getByRole('button', { name: 'signIn' }));

        expect(await screen.findByTestId('signInError')).toHaveTextContent('auth.invalid_credentials');
        expect(status()).toHaveTextContent('signedOut');
    });

    it('drops whatever was cached under a previous identity when someone signs in', async () => {
        mocked.currentSession.mockRejectedValue(unauthorized());
        mocked.signIn.mockResolvedValue(sessionFixture());
        const { client } = renderProbe();
        client.setQueryData(['delivery', 'current'], { id: 'd1' });
        await waitFor(() => expect(status()).toHaveTextContent('signedOut'));

        await userEvent.click(screen.getByRole('button', { name: 'signIn' }));

        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
        expect(client.getQueryData(['delivery', 'current'])).toBeUndefined();
    });

    it('clears every cached query when the operator signs out', async () => {
        mocked.currentSession.mockResolvedValue(sessionFixture());
        mocked.signOut.mockResolvedValue(undefined);
        const { client } = renderProbe();
        client.setQueryData(['delivery', 'current'], { id: 'd1' });
        client.setQueryData(['something', 'else'], 42);
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));

        await userEvent.click(screen.getByRole('button', { name: 'signOut' }));

        await waitFor(() => expect(status()).toHaveTextContent('signedOut'));
        expect(client.getQueryData(['delivery', 'current'])).toBeUndefined();
        expect(client.getQueryData(['something', 'else'])).toBeUndefined();
    });

    it('stays signed in and rejects when sign-out cannot reach the server', async () => {
        mocked.currentSession.mockResolvedValue(sessionFixture());
        mocked.signOut.mockRejectedValue(networkFailure());
        const { client } = renderProbe();
        client.setQueryData(['delivery', 'current'], { id: 'd1' });
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));

        await userEvent.click(screen.getByRole('button', { name: 'signOut' }));

        expect(await screen.findByTestId('signOutError')).toBeInTheDocument();
        expect(status()).toHaveTextContent('signedIn');
        expect(client.getQueryData(['delivery', 'current'])).toEqual({ id: 'd1' });
    });

    it('ends the session and clears the cache when the HTTP client reports a 401', async () => {
        mocked.currentSession.mockResolvedValue(sessionFixture());
        const { client } = renderProbe();
        client.setQueryData(['delivery', 'current'], { id: 'd1' });
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));

        act(() => sessionEvents.emitEnded());

        expect(status()).toHaveTextContent('signedOut');
        expect(client.getQueryData(['delivery', 'current'])).toBeUndefined();
    });

    it('ignores a late answer to the restore once the operator has signed in', async () => {
        let rejectRestore: (error: AppError) => void = () => undefined;
        mocked.currentSession.mockReturnValue(new Promise((_, reject) => (rejectRestore = reject)));
        mocked.signIn.mockResolvedValue(sessionFixture());
        renderProbe();

        await userEvent.click(screen.getByRole('button', { name: 'signIn' }));
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
        await act(async () => rejectRestore(unauthorized()));

        expect(status()).toHaveTextContent('signedIn');
    });

    it('stops listening for expiry once unmounted', async () => {
        mocked.currentSession.mockResolvedValue(sessionFixture());
        const { client, unmount } = renderProbe();
        await waitFor(() => expect(status()).toHaveTextContent('signedIn'));
        unmount();
        client.setQueryData(['delivery', 'current'], { id: 'd1' });

        act(() => sessionEvents.emitEnded());

        expect(client.getQueryData(['delivery', 'current'])).toEqual({ id: 'd1' });
    });
});

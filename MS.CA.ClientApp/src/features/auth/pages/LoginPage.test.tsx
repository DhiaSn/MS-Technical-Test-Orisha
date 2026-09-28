import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppError } from '@/core/errors';
import type { SessionState } from '@/features/auth/context/SessionContext';
import { mockLocation, navigation, resetNavigation } from '@/features/auth/testing/mockNavigation';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { createSessionStub, StubSessionProvider, type SessionStub } from '@/features/auth/testing/stubSession';
import { LoginPage } from './LoginPage';

jest.mock('next/navigation', () => jest.requireActual('@/features/auth/testing/mockNavigation').nextNavigationMock);

function renderLogin(state: SessionState = { status: 'signedOut' }): SessionStub {
    const stub = createSessionStub(state);
    render(
        <StubSessionProvider value={stub}>
            <LoginPage />
        </StubSessionProvider>
    );
    return stub;
}

async function submitCredentials() {
    await userEvent.type(screen.getByLabelText('Identifiant'), 'magasinier');
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'Reception2026{Enter}');
}

describe('LoginPage', () => {
    beforeEach(() => resetNavigation());

    it('shows the login form with a heading', () => {
        renderLogin();

        expect(screen.getByRole('heading', { name: 'Connexion' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Se connecter' })).toBeInTheDocument();
        expect(document.title).toBe('Connexion');
    });

    it('signs in with the typed credentials', async () => {
        const stub = renderLogin();
        stub.signIn.mockResolvedValue(sessionFixture());

        await submitCredentials();

        expect(stub.signIn).toHaveBeenCalledWith({ username: 'magasinier', password: 'Reception2026' });
    });

    it('shows the error and stays on the page when sign-in fails', async () => {
        const stub = renderLogin();
        stub.signIn.mockRejectedValue(new AppError({ kind: 'unauthorized', message: 'x', code: 'auth.invalid_credentials' }));

        await submitCredentials();

        expect(await screen.findByRole('alert')).toHaveTextContent('Identifiant ou mot de passe incorrect.');
        expect(navigation.replace).not.toHaveBeenCalled();
    });

    it('navigates to the safe returnTo once signed in', async () => {
        mockLocation('/login', 'returnTo=%2Freception%3Ftab%3D1');
        renderLogin({ status: 'signedIn', session: sessionFixture() });

        await waitFor(() => expect(navigation.replace).toHaveBeenCalledWith('/reception?tab=1'));
    });

    it('navigates to the reception page instead of an unsafe returnTo', async () => {
        mockLocation('/login', 'returnTo=%2F%2Fevil.example');
        renderLogin({ status: 'signedIn', session: sessionFixture() });

        await waitFor(() => expect(navigation.replace).toHaveBeenCalledWith('/reception'));
        expect(navigation.replace).toHaveBeenCalledTimes(1);
    });

    it('redirects an operator who is already signed in without showing the form', () => {
        renderLogin({ status: 'signedIn', session: sessionFixture() });

        expect(screen.queryByLabelText('Identifiant')).not.toBeInTheDocument();
        expect(navigation.replace).toHaveBeenCalledWith('/reception');
    });

    it('does not bounce anywhere while the session is still being restored', () => {
        renderLogin({ status: 'loading' });

        expect(screen.getByLabelText('Identifiant')).toBeInTheDocument();
        expect(navigation.replace).not.toHaveBeenCalled();
    });
});

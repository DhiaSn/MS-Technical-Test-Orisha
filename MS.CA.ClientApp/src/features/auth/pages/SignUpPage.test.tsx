import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { authService } from '@/api';
import { AppError } from '@/core/errors';
import type { SessionState } from '@/features/auth/context/SessionContext';
import { mockLocation, navigation, resetNavigation } from '@/features/auth/testing/mockNavigation';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { createSessionStub, StubSessionProvider, type SessionStub } from '@/features/auth/testing/stubSession';
import { renderWithProviders } from '@/shared/testing/renderWithProviders';
import { SignUpPage } from './SignUpPage';

jest.mock('next/navigation', () => jest.requireActual('@/features/auth/testing/mockNavigation').nextNavigationMock);
jest.mock('@/api', () => ({ authService: { passwordPolicy: jest.fn() } }));

const mockedPasswordPolicy = authService.passwordPolicy as jest.Mock;

function renderSignUp(state: SessionState = { status: 'signedOut' }): SessionStub {
    const stub = createSessionStub(state);
    renderWithProviders(
        <StubSessionProvider value={stub}>
            <SignUpPage />
        </StubSessionProvider>
    );
    return stub;
}

async function submitRegistration() {
    await userEvent.type(screen.getByLabelText('Identifiant'), 'nouvel-operateur');
    await userEvent.type(screen.getByLabelText('Nom affiché'), 'Nouvel opérateur');
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'Reception2026{Enter}');
}

describe('SignUpPage', () => {
    beforeEach(() => {
        resetNavigation();
        mockedPasswordPolicy.mockResolvedValue({
            minimumLength: 8,
            requireUppercase: true,
            requireLowercase: true,
            requireDigit: true
        });
    });

    it('shows the sign-up form with a heading', () => {
        renderSignUp();

        expect(screen.getByRole('heading', { name: 'Créer un compte' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Créer le compte' })).toBeInTheDocument();
        expect(document.title).toBe('Créer un compte');
    });

    it('links back to the login page', () => {
        renderSignUp();

        expect(screen.getByRole('link', { name: 'Se connecter' })).toHaveAttribute('href', '/login');
    });

    it('registers with the typed fields', async () => {
        const stub = renderSignUp();
        stub.register.mockResolvedValue(sessionFixture());

        await submitRegistration();

        expect(stub.register).toHaveBeenCalledWith({
            username: 'nouvel-operateur',
            displayName: 'Nouvel opérateur',
            password: 'Reception2026'
        });
    });

    it('shows the error and stays on the page when registration fails', async () => {
        const stub = renderSignUp();
        stub.register.mockRejectedValue(new AppError({ kind: 'validation', message: 'x', code: 'account.username_taken' }));

        await submitRegistration();

        expect(await screen.findByText('Cet identifiant est déjà utilisé.')).toBeInTheDocument();
        expect(navigation.replace).not.toHaveBeenCalled();
    });

    it('navigates to the safe returnTo once registered', async () => {
        mockLocation('/sign-up', 'returnTo=%2Freception%3Ftab%3D1');
        renderSignUp({ status: 'signedIn', session: sessionFixture() });

        await waitFor(() => expect(navigation.replace).toHaveBeenCalledWith('/reception?tab=1'));
    });

    it('redirects an operator who is already signed in without showing the form', () => {
        renderSignUp({ status: 'signedIn', session: sessionFixture() });

        expect(screen.queryByLabelText('Identifiant')).not.toBeInTheDocument();
        expect(navigation.replace).toHaveBeenCalledWith('/reception');
    });

    it('shows a spinner and no form while the session is still being restored', () => {
        renderSignUp({ status: 'loading' });

        expect(screen.getByText('Vérification de la session…')).toBeInTheDocument();
        expect(screen.queryByLabelText('Identifiant')).not.toBeInTheDocument();
    });
});

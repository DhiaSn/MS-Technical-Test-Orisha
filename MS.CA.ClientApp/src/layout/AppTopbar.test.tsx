import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppError } from '@/core/errors';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { createSessionStub, StubSessionProvider, type SessionStub } from '@/features/auth/testing/stubSession';
import { ToastProvider } from '@/shared/components';
import { AppTopbar } from './AppTopbar';

function signedInStub(): SessionStub {
    return createSessionStub({ status: 'signedIn', session: sessionFixture() });
}

function renderTopbar(stub: SessionStub = signedInStub()): SessionStub {
    render(
        <ToastProvider>
            <StubSessionProvider value={stub}>
                <AppTopbar />
            </StubSessionProvider>
        </ToastProvider>
    );
    return stub;
}

describe('AppTopbar', () => {
    it('shows the operator and a sign-out button', () => {
        renderTopbar();

        expect(screen.getByText('Connecté : Magasinier')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Se déconnecter' })).toBeEnabled();
    });

    it('shows no operator and no sign-out button before there is a session', () => {
        renderTopbar(createSessionStub({ status: 'loading' }));

        expect(screen.queryByRole('button', { name: 'Se déconnecter' })).not.toBeInTheDocument();
        expect(screen.getByText('Réception logistique')).toBeInTheDocument();
    });

    it('signs out when the button is pressed, and is disabled meanwhile', async () => {
        const stub = signedInStub();
        stub.signOut.mockReturnValue(new Promise(() => undefined));
        renderTopbar(stub);

        await userEvent.click(screen.getByRole('button', { name: 'Se déconnecter' }));

        expect(stub.signOut).toHaveBeenCalledTimes(1);
        expect(screen.getByRole('button', { name: 'Se déconnecter' })).toBeDisabled();
    });

    it('tells the operator when sign-out failed and lets them try again', async () => {
        const stub = signedInStub();
        stub.signOut.mockRejectedValue(new AppError({ kind: 'network', message: 'x' }));
        renderTopbar(stub);

        await userEvent.click(screen.getByRole('button', { name: 'Se déconnecter' }));

        expect(await screen.findByText('Déconnexion impossible. Réessayez.')).toBeInTheDocument();
        await waitFor(() => expect(screen.getByRole('button', { name: 'Se déconnecter' })).toBeEnabled());
    });
});

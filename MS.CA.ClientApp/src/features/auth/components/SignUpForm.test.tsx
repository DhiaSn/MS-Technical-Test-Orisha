import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { authService } from '@/api';
import { AppError } from '@/core/errors';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { renderWithProviders } from '@/shared/testing/renderWithProviders';
import { SignUpForm } from './SignUpForm';

jest.mock('@/api', () => ({ authService: { passwordPolicy: jest.fn() } }));

const mockedPasswordPolicy = authService.passwordPolicy as jest.Mock;

function deferred<T>() {
    let resolve: (value: T) => void = () => undefined;
    const promise = new Promise<T>((res) => (resolve = res));
    return { promise, resolve };
}

const defaultPolicy = { minimumLength: 8, requireUppercase: true, requireLowercase: true, requireDigit: true };

async function fillIn({ username = 'nouvel-operateur', displayName = 'Nouvel opérateur', password = 'Reception2026' } = {}) {
    await userEvent.type(screen.getByLabelText('Identifiant'), username);
    await userEvent.type(screen.getByLabelText('Nom affiché'), displayName);
    await userEvent.type(screen.getByLabelText('Mot de passe'), password);
}

describe('SignUpForm', () => {
    beforeEach(() => {
        jest.resetAllMocks();
        mockedPasswordPolicy.mockResolvedValue(defaultPolicy);
    });

    it('submits the typed username, display name and password once', async () => {
        const onSubmit = jest.fn().mockResolvedValue(undefined);
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);

        await fillIn();
        await userEvent.type(screen.getByLabelText('Mot de passe'), '{Enter}');

        expect(onSubmit).toHaveBeenCalledTimes(1);
        expect(onSubmit).toHaveBeenCalledWith({
            username: 'nouvel-operateur',
            displayName: 'Nouvel opérateur',
            password: 'Reception2026'
        });
    });

    it('does not send a second request while the first is pending', async () => {
        const pending = deferred<void>();
        const onSubmit = jest.fn().mockReturnValue(pending.promise);
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);
        await fillIn();

        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));
        await userEvent.click(screen.getByRole('button', { name: 'Création…' }));

        expect(onSubmit).toHaveBeenCalledTimes(1);
        expect(screen.getByRole('button', { name: 'Création…' })).toBeDisabled();
    });

    it('shows the password hint built from the server policy', async () => {
        mockedPasswordPolicy.mockResolvedValue({
            minimumLength: 10,
            requireUppercase: true,
            requireLowercase: false,
            requireDigit: true
        });
        renderWithProviders(<SignUpForm onSubmit={jest.fn()} />);

        expect(await screen.findByText('Au moins 10 caractères, dont une majuscule et un chiffre.')).toBeInTheDocument();
    });

    it('hides the password hint when the policy request fails', async () => {
        mockedPasswordPolicy.mockRejectedValue(new Error('unreachable'));
        renderWithProviders(<SignUpForm onSubmit={jest.fn()} />);

        await waitFor(() => expect(mockedPasswordPolicy).toHaveBeenCalled());
        expect(screen.queryByText(/Au moins/)).not.toBeInTheDocument();
    });

    it('asks for the missing fields without calling the server', async () => {
        const onSubmit = jest.fn();
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);

        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));

        expect(onSubmit).not.toHaveBeenCalled();
        expect(screen.getAllByText('Ce champ est obligatoire.')).toHaveLength(3);
    });

    it('shows a taken username under the username field, not as a top-level alert', async () => {
        const onSubmit = jest.fn().mockRejectedValue(new AppError({ kind: 'validation', message: 'x', code: 'account.username_taken' }));
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);
        await fillIn();

        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));

        await screen.findByText('Cet identifiant est déjà utilisé.');
        expect(screen.getByLabelText('Identifiant')).toHaveAttribute('aria-invalid', 'true');
        expect(screen.queryByText('Une erreur inattendue est survenue.')).not.toBeInTheDocument();
    });

    it('lists every password error at once when the server rejects several rules', async () => {
        const onSubmit = jest.fn().mockRejectedValue(
            new AppError({
                kind: 'validation',
                message: 'x',
                code: 'validation.failed',
                fieldCodes: {
                    password: [{ code: 'password.too_short', params: { min: '8' } }, { code: 'password.missing_uppercase' }]
                }
            })
        );
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);
        await fillIn();

        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));

        expect(await screen.findByText('Le mot de passe doit contenir au moins 8 caractères.')).toBeInTheDocument();
        expect(screen.getByText('Le mot de passe doit contenir au moins une majuscule.')).toBeInTheDocument();
    });

    it('shows the generic message for any other failure', async () => {
        const onSubmit = jest.fn().mockRejectedValue(new AppError({ kind: 'server', message: 'x', status: 500 }));
        renderWithProviders(<SignUpForm onSubmit={onSubmit} />);
        await fillIn();

        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));

        expect(await screen.findByRole('alert')).toHaveTextContent('Une erreur est survenue côté serveur. Réessayez dans un instant.');
    });

    it('declares the attributes password managers look for', async () => {
        renderWithProviders(<SignUpForm onSubmit={jest.fn()} />);

        expect(screen.getByLabelText('Identifiant')).toHaveAttribute('autocomplete', 'username');
        expect(screen.getByLabelText('Nom affiché')).toHaveAttribute('autocomplete', 'name');
        expect(screen.getByLabelText('Mot de passe')).toHaveAttribute('autocomplete', 'new-password');
        expect(screen.getByLabelText('Mot de passe')).toHaveAttribute('type', 'password');
    });

    it('has no accessibility violations, including with errors showing', async () => {
        const onSubmit = jest.fn().mockRejectedValue(new AppError({ kind: 'validation', message: 'x', code: 'account.username_taken' }));
        const { container } = renderWithProviders(<SignUpForm onSubmit={onSubmit} />);
        await fillIn();
        await userEvent.click(screen.getByRole('button', { name: 'Créer le compte' }));
        await screen.findByText('Cet identifiant est déjà utilisé.');

        await expectNoA11yViolations(container);
    });
});

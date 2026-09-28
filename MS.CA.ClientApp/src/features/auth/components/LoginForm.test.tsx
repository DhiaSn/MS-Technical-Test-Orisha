import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppError } from '@/core/errors';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { LoginForm } from './LoginForm';

function deferred<T>() {
    let resolve: (value: T) => void = () => undefined;
    const promise = new Promise<T>((res) => (resolve = res));
    return { promise, resolve };
}

async function fillIn(password: string) {
    await userEvent.type(screen.getByLabelText('Identifiant'), 'magasinier');
    await userEvent.type(screen.getByLabelText('Mot de passe'), password);
}

describe('LoginForm', () => {
    it('submits the typed username and password once', async () => {
        const onSubmit = jest.fn().mockResolvedValue(undefined);
        render(<LoginForm onSubmit={onSubmit} />);

        await fillIn('Reception2026{Enter}');

        expect(onSubmit).toHaveBeenCalledTimes(1);
        expect(onSubmit).toHaveBeenCalledWith({ username: 'magasinier', password: 'Reception2026' });
    });

    it('does not send a second request while the first is pending', async () => {
        const pending = deferred<void>();
        const onSubmit = jest.fn().mockReturnValue(pending.promise);
        render(<LoginForm onSubmit={onSubmit} />);
        await fillIn('Reception2026');

        await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }));
        await userEvent.click(screen.getByRole('button', { name: 'Connexion…' }));
        await userEvent.type(screen.getByLabelText('Mot de passe'), '{Enter}');

        expect(onSubmit).toHaveBeenCalledTimes(1);
        expect(screen.getByRole('button', { name: 'Connexion…' })).toBeDisabled();
    });

    it('shows one generic message, keeps the username, clears the password and refocuses it', async () => {
        const onSubmit = jest
            .fn()
            .mockRejectedValue(new AppError({ kind: 'unauthorized', message: 'x', code: 'auth.invalid_credentials' }));
        render(<LoginForm onSubmit={onSubmit} />);

        await fillIn('Wrong-Password1{Enter}');

        expect(await screen.findByRole('alert')).toHaveTextContent('Identifiant ou mot de passe incorrect.');
        expect(screen.getByLabelText('Identifiant')).toHaveValue('magasinier');
        expect(screen.getByLabelText('Mot de passe')).toHaveValue('');
        expect(screen.getByLabelText('Mot de passe')).toHaveFocus();
        expect(screen.getByRole('button', { name: 'Se connecter' })).toBeEnabled();
    });

    it('lets the operator try again after a failure and clears the previous message', async () => {
        const onSubmit = jest
            .fn()
            .mockRejectedValueOnce(new AppError({ kind: 'unauthorized', message: 'x', code: 'auth.invalid_credentials' }))
            .mockReturnValueOnce(new Promise(() => undefined));
        render(<LoginForm onSubmit={onSubmit} />);
        await fillIn('Wrong-Password1{Enter}');
        await screen.findByRole('alert');

        await userEvent.type(screen.getByLabelText('Mot de passe'), 'Reception2026{Enter}');

        expect(onSubmit).toHaveBeenCalledTimes(2);
        expect(onSubmit).toHaveBeenLastCalledWith({ username: 'magasinier', password: 'Reception2026' });
        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('tells a throttled operator to wait, distinctly from wrong credentials', async () => {
        const onSubmit = jest
            .fn()
            .mockRejectedValue(new AppError({ kind: 'unknown', message: 'x', status: 429, code: 'rate_limit.exceeded' }));
        render(<LoginForm onSubmit={onSubmit} />);

        await fillIn('Reception2026{Enter}');

        const alert = await screen.findByRole('alert');
        expect(alert).toHaveTextContent('Trop de tentatives. Réessayez dans une minute.');
        expect(alert).not.toHaveTextContent('incorrect');
    });

    it('reports an unreachable server in French', async () => {
        const onSubmit = jest.fn().mockRejectedValue(new AppError({ kind: 'network', message: 'x' }));
        render(<LoginForm onSubmit={onSubmit} />);

        await fillIn('Reception2026{Enter}');

        expect(await screen.findByRole('alert')).toHaveTextContent('Impossible de joindre le serveur.');
    });

    it('asks for the missing fields without calling the server', async () => {
        const onSubmit = jest.fn();
        render(<LoginForm onSubmit={onSubmit} />);

        await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }));

        expect(onSubmit).not.toHaveBeenCalled();
        expect(screen.getAllByText('Ce champ est obligatoire.')).toHaveLength(2);
    });

    it('does not accept a blank username', async () => {
        const onSubmit = jest.fn();
        render(<LoginForm onSubmit={onSubmit} />);

        await userEvent.type(screen.getByLabelText('Identifiant'), '   ');
        await userEvent.type(screen.getByLabelText('Mot de passe'), 'Reception2026{Enter}');

        expect(onSubmit).not.toHaveBeenCalled();
        expect(screen.getAllByText('Ce champ est obligatoire.')).toHaveLength(1);
    });

    it('declares the attributes password managers look for', () => {
        render(<LoginForm onSubmit={jest.fn()} />);

        expect(screen.getByLabelText('Identifiant')).toHaveAttribute('autocomplete', 'username');
        expect(screen.getByLabelText('Identifiant')).toHaveAttribute('name', 'username');
        expect(screen.getByLabelText('Mot de passe')).toHaveAttribute('type', 'password');
        expect(screen.getByLabelText('Mot de passe')).toHaveAttribute('name', 'password');
        expect(screen.getByLabelText('Mot de passe')).toHaveAttribute('autocomplete', 'current-password');
    });

    it('has no accessibility violations, including with an error showing', async () => {
        const onSubmit = jest
            .fn()
            .mockRejectedValue(new AppError({ kind: 'unauthorized', message: 'x', code: 'auth.invalid_credentials' }));
        const { container } = render(<LoginForm onSubmit={onSubmit} />);
        await fillIn('Wrong-Password1{Enter}');
        await screen.findByRole('alert');

        await expectNoA11yViolations(container);
    });
});

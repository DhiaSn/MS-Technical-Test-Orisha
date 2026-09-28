import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { Alert } from './Alert';
import { ApiErrorState } from './ApiErrorState';
import { Button } from './Button';
import { EmptyState } from './EmptyState';
import { Spinner } from './Spinner';
import { ToastProvider, useToast } from './Toast';

describe('Button', () => {
    it('is a non-submitting button by default', () => {
        render(<Button>Valider</Button>);

        expect(screen.getByRole('button', { name: 'Valider' })).toHaveAttribute('type', 'button');
    });

    it('calls its handler on click and not when disabled', async () => {
        const onClick = jest.fn();
        const { rerender } = render(<Button onClick={onClick}>Valider</Button>);

        await userEvent.click(screen.getByRole('button'));
        rerender(
            <Button onClick={onClick} disabled>
                Valider
            </Button>
        );
        await userEvent.click(screen.getByRole('button'));

        expect(onClick).toHaveBeenCalledTimes(1);
    });

    it('renders both variants without accessibility violations', async () => {
        const { container } = render(
            <>
                <Button variant="primary">Un</Button>
                <Button variant="secondary">Deux</Button>
            </>
        );

        expect(screen.getAllByRole('button')).toHaveLength(2);
        await expectNoA11yViolations(container);
    });
});

describe('Spinner', () => {
    it('is a status with a visually hidden label', () => {
        render(<Spinner label="Chargement…" />);

        expect(screen.getByRole('status')).toHaveTextContent('Chargement…');
    });
});

describe('Alert', () => {
    it('announces an error as an alert', async () => {
        const { container } = render(<Alert tone="error">Échec</Alert>);

        expect(screen.getByRole('alert')).toHaveTextContent('Échec');
        await expectNoA11yViolations(container);
    });

    it('announces information as a status', () => {
        render(<Alert tone="info">Sauvegardé</Alert>);

        expect(screen.getByRole('status')).toHaveTextContent('Sauvegardé');
    });

    it('names its tone so colour is not the only signal', () => {
        render(<Alert tone="error">Échec</Alert>);

        expect(screen.getByRole('alert')).toHaveTextContent(`${translate('tone.error')} : Échec`);
    });
});

describe('EmptyState', () => {
    it('shows its title and description', () => {
        render(<EmptyState title="Aucune commande" description="Rien à réceptionner." />);

        expect(screen.getByRole('heading', { name: 'Aucune commande' })).toBeInTheDocument();
        expect(screen.getByText('Rien à réceptionner.')).toBeInTheDocument();
    });

    it('renders its action only when given one', () => {
        const { rerender } = render(<EmptyState title="Vide" />);
        expect(screen.queryByRole('button')).not.toBeInTheDocument();

        rerender(<EmptyState title="Vide" action={<Button>Actualiser</Button>} />);
        expect(screen.getByRole('button', { name: 'Actualiser' })).toBeInTheDocument();
    });
});

describe('ApiErrorState', () => {
    it('describes the failure in French and offers a retry', async () => {
        const onRetry = jest.fn();
        const error = new AppError({ kind: 'network', message: 'Failed to fetch' });
        const { container } = render(<ApiErrorState error={error} onRetry={onRetry} />);

        expect(screen.getByText(translate('error.network'))).toBeInTheDocument();
        expect(screen.queryByText('Failed to fetch')).not.toBeInTheDocument();

        await userEvent.click(screen.getByRole('button', { name: translate('action.retry') }));

        expect(onRetry).toHaveBeenCalledTimes(1);
        await expectNoA11yViolations(container);
    });

    it('uses the given title, or a generic one', () => {
        const error = new AppError({ kind: 'server', message: 'boom', status: 500 });
        const { rerender } = render(<ApiErrorState error={error} onRetry={jest.fn()} />);
        expect(screen.getByRole('heading', { name: translate('error.loadFailed') })).toBeInTheDocument();

        rerender(<ApiErrorState title="Commande indisponible" error={error} onRetry={jest.fn()} />);
        expect(screen.getByRole('heading', { name: 'Commande indisponible' })).toBeInTheDocument();
    });
});

describe('Toast', () => {
    function Trigger() {
        const toast = useToast();
        return (
            <>
                <button onClick={() => toast.error('Échec de la sauvegarde')}>error</button>
                <button onClick={() => toast.info('Sauvegardé')}>info</button>
            </>
        );
    }

    function renderToasts() {
        return render(
            <ToastProvider>
                <Trigger />
            </ToastProvider>
        );
    }

    afterEach(() => {
        jest.useRealTimers();
    });

    it('shows a message in a live region', async () => {
        renderToasts();

        await userEvent.click(screen.getByRole('button', { name: 'error' }));

        expect(screen.getByRole('status')).toHaveTextContent('Échec de la sauvegarde');
    });

    it('disappears by itself after five seconds', async () => {
        jest.useFakeTimers();
        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        renderToasts();

        await user.click(screen.getByRole('button', { name: 'info' }));
        act(() => {
            jest.advanceTimersByTime(4900);
        });
        expect(screen.getByText('Sauvegardé')).toBeInTheDocument();

        act(() => {
            jest.advanceTimersByTime(200);
        });
        expect(screen.queryByText('Sauvegardé')).not.toBeInTheDocument();
    });

    it('can be dismissed', async () => {
        renderToasts();

        await userEvent.click(screen.getByRole('button', { name: 'info' }));
        await userEvent.click(screen.getByRole('button', { name: translate('toast.dismiss') }));

        expect(screen.queryByText('Sauvegardé')).not.toBeInTheDocument();
    });

    it('fails loudly when used outside its provider', () => {
        const spy = jest.spyOn(console, 'error').mockImplementation(() => undefined);

        expect(() => render(<Trigger />)).toThrow('ToastProvider');
        spy.mockRestore();
    });
});

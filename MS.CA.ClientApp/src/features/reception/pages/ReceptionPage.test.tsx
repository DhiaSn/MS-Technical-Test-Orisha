import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { receptionService } from '@/api';
import { AppError } from '@/core/errors';
import { deliveryFixture, deliveryId, palletId } from '@/features/reception/testing/deliveryFixture';
import { renderWithProviders } from '@/shared/testing/renderWithProviders';
import { ReceptionPage } from './ReceptionPage';

jest.mock('@/api', () => ({
    receptionService: {
        setPalletValidated: jest.fn(),
        setCartonValidated: jest.fn(),
        setProductValidated: jest.fn(),
        setReceivedQuantity: jest.fn(),
        getCurrent: jest.fn()
    }
}));

const service = jest.mocked(receptionService);

const renderPage = () => renderWithProviders(<ReceptionPage />);

beforeEach(() => {
    jest.resetAllMocks();
});

describe('ReceptionPage', () => {
    it('shows a loading state, then the delivery header and the collapsed tree', async () => {
        service.getCurrent.mockResolvedValue(deliveryFixture());
        renderPage();

        expect(screen.getByText('Chargement de la commande…')).toBeInTheDocument();
        expect(await screen.findByRole('heading', { name: 'Réception de la commande CMD-2026' })).toBeInTheDocument();
        expect(screen.getByText('0 / 180 articles reçus')).toBeInTheDocument();
    });

    it('shows the state the server returns after a click — even if it is not what the click implies', async () => {
        service.getCurrent.mockResolvedValue(deliveryFixture());
        service.setPalletValidated.mockResolvedValue(deliveryFixture({ palletStatus: 'partial', received: 60 }));
        renderPage();

        await userEvent.click(await screen.findByRole('checkbox', { name: 'Valider la palette PAL-01' }));

        await waitFor(() => expect(screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' })).toBePartiallyChecked());
        expect(screen.getByText('60 / 180 articles reçus')).toBeInTheDocument();
        expect(service.setPalletValidated).toHaveBeenCalledWith(deliveryId, palletId, true);
    });

    it('recovers from a failed save: toast in French and the true state is fetched again', async () => {
        service.getCurrent.mockResolvedValue(deliveryFixture());
        service.setPalletValidated.mockRejectedValue(new AppError({ kind: 'network', message: 'x' }));
        renderPage();

        await userEvent.click(await screen.findByRole('checkbox', { name: 'Valider la palette PAL-01' }));

        expect(await screen.findByText(/Enregistrement impossible/)).toBeInTheDocument();
        await waitFor(() => expect(service.getCurrent).toHaveBeenCalledTimes(2));
        expect(screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' })).not.toBeChecked();
    });

    it('shows an empty state when there is no delivery (404)', async () => {
        service.getCurrent.mockRejectedValue(new AppError({ kind: 'notFound', message: 'x', code: 'reception.delivery_not_found' }));
        renderPage();

        expect(await screen.findByText('Aucune commande en attente')).toBeInTheDocument();
    });

    it('shows a retryable error state when the server is unreachable', async () => {
        service.getCurrent.mockRejectedValueOnce(new AppError({ kind: 'network', message: 'x' })).mockResolvedValueOnce(deliveryFixture());
        renderPage();

        expect(await screen.findByText('Impossible de charger la commande')).toBeInTheDocument();
        await userEvent.click(screen.getByRole('button', { name: 'Réessayer' }));

        expect(await screen.findByRole('heading', { name: /Réception de la commande/ })).toBeInTheDocument();
    });
});

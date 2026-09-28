import { act, screen, waitFor } from '@testing-library/react';
import { receptionService } from '@/api';
import { AppError } from '@/core/errors';
import { deliveryFixture } from '@/features/reception/testing/deliveryFixture';
import { renderMutations } from '@/features/reception/testing/renderMutations';
import type { Delivery } from '@/features/reception/types';
import { deliveryKeys } from './deliveryKeys';

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

const deferred = <T,>() => {
    let resolve!: (value: T) => void;
    let reject!: (reason: unknown) => void;
    const promise = new Promise<T>((res, rej) => {
        resolve = res;
        reject = rej;
    });
    return { promise, resolve, reject };
};

beforeEach(() => {
    jest.resetAllMocks();
});

describe('useDeliveryMutations', () => {
    it('applies the delivery the server returns', async () => {
        const updated = deliveryFixture({ palletStatus: 'partial', received: 60 });
        service.setCartonValidated.mockResolvedValue(updated);
        const { result, client } = renderMutations('d1');

        await act(() => result.current.toggleCarton('c1', true));

        expect(client.getQueryData(deliveryKeys.current)).toEqual(updated);
        expect(service.setCartonValidated).toHaveBeenCalledWith('d1', 'c1', true);
    });

    it.each([
        ['togglePallet', 'setPalletValidated'],
        ['toggleProduct', 'setProductValidated']
    ] as const)('%s sends the matching command', async (method, command) => {
        service[command].mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1');

        await act(() => result.current[method]('n1', false));

        expect(service[command]).toHaveBeenCalledWith('d1', 'n1', false);
    });

    it('sends a received quantity', async () => {
        service.setReceivedQuantity.mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1');

        const outcome = await act(() => result.current.setQuantity('p1', 12));

        expect(outcome).toBeUndefined();
        expect(service.setReceivedQuantity).toHaveBeenCalledWith('d1', 'p1', 12);
    });

    it('sends two rapid commands one after the other', async () => {
        const first = deferred<Delivery>();
        const second = deferred<Delivery>();
        service.setProductValidated.mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
        const { result } = renderMutations('d1');

        act(() => {
            void result.current.toggleProduct('p1', true);
            void result.current.toggleProduct('p2', true);
        });
        await waitFor(() => expect(service.setProductValidated).toHaveBeenCalledTimes(1));

        await act(async () => first.resolve(deliveryFixture()));
        await waitFor(() => expect(service.setProductValidated).toHaveBeenCalledTimes(2));
        expect(service.setProductValidated).toHaveBeenLastCalledWith('d1', 'p2', true);
    });

    it('serialises commands of different kinds on the same delivery', async () => {
        const first = deferred<Delivery>();
        service.setCartonValidated.mockReturnValueOnce(first.promise);
        service.setReceivedQuantity.mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1');

        act(() => {
            void result.current.toggleCarton('c1', true);
            void result.current.setQuantity('p1', 3);
        });
        await waitFor(() => expect(service.setCartonValidated).toHaveBeenCalledTimes(1));
        expect(service.setReceivedQuantity).not.toHaveBeenCalled();

        await act(async () => first.resolve(deliveryFixture()));
        await waitFor(() => expect(service.setReceivedQuantity).toHaveBeenCalledTimes(1));
    });

    it('marks the node pending while its request is in flight', async () => {
        const pending = deferred<Delivery>();
        service.setPalletValidated.mockReturnValue(pending.promise);
        const { result } = renderMutations('d1');

        act(() => {
            void result.current.togglePallet('pal1', true);
        });
        await waitFor(() => expect(result.current.isPending('pal1')).toBe(true));
        expect(result.current.isPending('other')).toBe(false);

        await act(async () => pending.resolve(deliveryFixture()));
        await waitFor(() => expect(result.current.isPending('pal1')).toBe(false));
    });

    it('keeps a node pending until its last queued command has answered', async () => {
        const first = deferred<Delivery>();
        const second = deferred<Delivery>();
        service.setProductValidated.mockReturnValueOnce(first.promise);
        service.setReceivedQuantity.mockReturnValueOnce(second.promise);
        const { result } = renderMutations('d1');

        act(() => {
            void result.current.toggleProduct('p1', true);
            void result.current.setQuantity('p1', 4);
        });
        await act(async () => first.resolve(deliveryFixture()));

        expect(result.current.isPending('p1')).toBe(true);

        await act(async () => second.resolve(deliveryFixture()));
        await waitFor(() => expect(result.current.isPending('p1')).toBe(false));
    });

    it('reports a failure with a toast and refetches the real state', async () => {
        service.setCartonValidated.mockRejectedValue(new AppError({ kind: 'server', message: 'x' }));
        service.getCurrent.mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1', { seed: deliveryFixture(), observe: true });
        await waitFor(() => expect(service.getCurrent).toHaveBeenCalledTimes(1));

        await act(() => result.current.toggleCarton('c1', true));

        expect(await screen.findByText(/Enregistrement impossible/)).toBeInTheDocument();
        await waitFor(() => expect(service.getCurrent).toHaveBeenCalledTimes(2));
    });

    it('returns the failure to the caller', async () => {
        const failure = new AppError({ kind: 'server', message: 'x' });
        service.setCartonValidated.mockRejectedValue(failure);
        const { result } = renderMutations('d1');

        const outcome = await act(() => result.current.toggleCarton('c1', true));

        expect(outcome).toBe(failure);
    });

    it('lets the queued commands answer before refetching after a failure', async () => {
        const first = deferred<Delivery>();
        service.setCartonValidated.mockReturnValueOnce(first.promise);
        service.setProductValidated.mockResolvedValue(deliveryFixture({ palletStatus: 'partial' }));
        service.getCurrent.mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1', { seed: deliveryFixture(), observe: true });
        await waitFor(() => expect(service.getCurrent).toHaveBeenCalledTimes(1));

        act(() => {
            void result.current.toggleCarton('c1', true);
            void result.current.toggleProduct('p1', true);
        });
        await act(async () => first.reject(new AppError({ kind: 'server', message: 'x' })));

        await waitFor(() => expect(service.setProductValidated).toHaveBeenCalledTimes(1));
        expect(await screen.findByText(/Enregistrement impossible/)).toBeInTheDocument();
        expect(service.getCurrent).toHaveBeenCalledTimes(1);
    });

    it('does not toast when the session has expired: the guard takes over', async () => {
        service.setCartonValidated.mockRejectedValue(new AppError({ kind: 'unauthorized', message: 'x', status: 401 }));
        service.getCurrent.mockResolvedValue(deliveryFixture());
        const { result } = renderMutations('d1', { seed: deliveryFixture(), observe: true });
        await waitFor(() => expect(service.getCurrent).toHaveBeenCalledTimes(1));

        await act(() => result.current.toggleCarton('c1', true));

        expect(screen.queryByText(/Enregistrement impossible/)).not.toBeInTheDocument();
        expect(service.getCurrent).toHaveBeenCalledTimes(1);
    });

    it('returns a validation error from setQuantity instead of toasting it', async () => {
        const error = new AppError({
            kind: 'validation',
            message: 'x',
            code: 'validation.failed',
            fieldCodes: { receivedQuantity: [{ code: 'reception.quantity_out_of_range', params: { min: '0', max: '50' } }] }
        });
        service.setReceivedQuantity.mockRejectedValue(error);
        const { result } = renderMutations('d1');

        const outcome = await act(() => result.current.setQuantity('p1', 999));

        expect(outcome).toBe(error);
        expect(screen.queryByText(/Enregistrement impossible/)).not.toBeInTheDocument();
    });
});

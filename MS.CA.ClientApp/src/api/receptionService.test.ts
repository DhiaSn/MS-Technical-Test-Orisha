import { httpClient } from './httpClient';
import { receptionService } from './receptionService';

jest.mock('./httpClient', () => ({ httpClient: { get: jest.fn(), put: jest.fn() } }));

const get = jest.mocked(httpClient.get);
const put = jest.mocked(httpClient.put);

describe('receptionService', () => {
    beforeEach(() => {
        get.mockReset();
        put.mockReset();
    });

    it('reads the current delivery', async () => {
        const signal = new AbortController().signal;

        await receptionService.getCurrent(signal);

        expect(get).toHaveBeenCalledWith('/reception/deliveries/current', { signal });
    });

    it('validates a pallet', async () => {
        await receptionService.setPalletValidated('d1', 'p1', true);

        expect(put).toHaveBeenCalledWith('/reception/deliveries/d1/pallets/p1/validation', { validated: true });
    });

    it('validates a carton', async () => {
        await receptionService.setCartonValidated('d1', 'c1', true);

        expect(put).toHaveBeenCalledWith('/reception/deliveries/d1/cartons/c1/validation', { validated: true });
    });

    it('un-validates a product', async () => {
        await receptionService.setProductValidated('d1', 'p1', false);

        expect(put).toHaveBeenCalledWith('/reception/deliveries/d1/products/p1/validation', { validated: false });
    });

    it('sets a received quantity', async () => {
        await receptionService.setReceivedQuantity('d1', 'p1', 4);

        expect(put).toHaveBeenCalledWith('/reception/deliveries/d1/products/p1/received-quantity', { receivedQuantity: 4 });
    });

    it('encodes the ids it puts in a path', async () => {
        await receptionService.setCartonValidated('d/1', 'c 1', true);

        expect(put).toHaveBeenCalledWith('/reception/deliveries/d%2F1/cartons/c%201/validation', { validated: true });
    });
});

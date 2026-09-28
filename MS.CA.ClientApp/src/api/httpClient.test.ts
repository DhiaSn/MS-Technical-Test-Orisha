import { HttpClient } from './httpClient';
import { sessionEvents } from './sessionEvents';

const json = (body: unknown, init?: ResponseInit) =>
    new Response(JSON.stringify(body), { status: 200, headers: { 'content-type': 'application/json' }, ...init });

const unauthorized = () =>
    new Response(JSON.stringify({ code: 'auth.unauthenticated' }), {
        status: 401,
        headers: { 'content-type': 'application/problem+json' }
    });

describe('HttpClient', () => {
    const fetchMock = jest.fn();
    const headerOf = (call: number, name: string) =>
        new Headers((fetchMock.mock.calls[call] as [string, RequestInit])[1].headers).get(name);

    beforeEach(() => {
        fetchMock.mockReset();
        global.fetch = fetchMock;
    });

    it('sends a GET to the base path and parses JSON', async () => {
        fetchMock.mockResolvedValue(json({ ok: true }));

        await expect(new HttpClient({ baseUrl: '/api/core' }).get('/reception/deliveries/current')).resolves.toEqual({ ok: true });

        expect(fetchMock).toHaveBeenCalledWith('/api/core/reception/deliveries/current', expect.objectContaining({ method: 'GET' }));
    });

    it('sends a PUT with a JSON body and content type', async () => {
        fetchMock.mockResolvedValue(json({}));

        await new HttpClient({ baseUrl: '/api/core' }).put('/x', { validated: true });

        const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
        expect(init).toMatchObject({ method: 'PUT', body: '{"validated":true}' });
        expect(new Headers(init.headers).get('content-type')).toBe('application/json');
    });

    it('rejects with an AppError carrying the problem code', async () => {
        fetchMock.mockResolvedValue(
            new Response(JSON.stringify({ code: 'reception.pallet_not_found' }), {
                status: 404,
                headers: { 'content-type': 'application/problem+json' }
            })
        );

        await expect(new HttpClient({ baseUrl: '/api/core' }).put('/x', {})).rejects.toMatchObject({
            kind: 'notFound',
            code: 'reception.pallet_not_found'
        });
    });

    it('rejects with a network error when fetch itself fails', async () => {
        fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

        await expect(new HttpClient({ baseUrl: '/api/core' }).get('/x')).rejects.toMatchObject({ kind: 'network' });
    });

    it('sends the CSRF header on writes and not on reads', async () => {
        fetchMock.mockImplementation(() => Promise.resolve(json({})));
        const client = new HttpClient({ baseUrl: '/api/core' });

        await client.get('/x');
        await client.put('/x', {});
        await client.post('/x');

        expect([0, 1, 2].map((call) => headerOf(call, 'x-ms-csrf'))).toEqual([null, '1', '1']);
    });

    it('sends the CSRF header on sign-in, where the session is not consulted', async () => {
        fetchMock.mockResolvedValue(json({}));

        await new HttpClient({ baseUrl: '/api/core' }).post(
            '/identity/auth/sign-in',
            { username: 'u', password: 'p' },
            { sessionAware: false }
        );

        expect(headerOf(0, 'x-ms-csrf')).toBe('1');
    });

    it('sends the session cookie to its own origin only', async () => {
        fetchMock.mockResolvedValue(json({}));

        await new HttpClient({ baseUrl: '/api/core' }).get('/x');

        expect((fetchMock.mock.calls[0] as [string, RequestInit])[1]).toMatchObject({ credentials: 'same-origin' });
    });

    it('returns undefined for a 204 No Content answer', async () => {
        fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

        await expect(new HttpClient({ baseUrl: '/api/core' }).post('/identity/auth/sign-out')).resolves.toBeUndefined();
    });

    describe('a 401 answer', () => {
        const ended = jest.fn();
        let unsubscribe: () => void;

        beforeEach(() => {
            ended.mockReset();
            unsubscribe = sessionEvents.onEnded(ended);
        });

        afterEach(() => unsubscribe());

        it('ends the session by default', async () => {
            fetchMock.mockResolvedValueOnce(unauthorized());

            await expect(new HttpClient({ baseUrl: '/api/core' }).get('/reception/deliveries/current')).rejects.toMatchObject({
                kind: 'unauthorized'
            });

            expect(ended).toHaveBeenCalledTimes(1);
        });

        it('leaves the session alone for a request that opts out, such as sign-in with bad credentials', async () => {
            fetchMock.mockResolvedValueOnce(unauthorized());

            await expect(
                new HttpClient({ baseUrl: '/api/core' }).post('/identity/auth/sign-in', {}, { sessionAware: false })
            ).rejects.toMatchObject({ kind: 'unauthorized' });

            expect(ended).not.toHaveBeenCalled();
        });

        it('is not reported for other failures', async () => {
            fetchMock.mockResolvedValueOnce(new Response('{}', { status: 500 }));

            await expect(new HttpClient({ baseUrl: '/api/core' }).get('/x')).rejects.toMatchObject({ kind: 'server' });

            expect(ended).not.toHaveBeenCalled();
        });
    });

    it('rejects with a timeout error when the server never answers', async () => {
        fetchMock.mockImplementation(
            (_url: string, init: RequestInit) =>
                new Promise((_resolve, reject) => init.signal?.addEventListener('abort', () => reject(init.signal?.reason)))
        );

        await expect(new HttpClient({ baseUrl: '/api/core', timeoutMs: 10 }).get('/x')).rejects.toMatchObject({ kind: 'timeout' });
    });
});

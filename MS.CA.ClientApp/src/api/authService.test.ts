import { authService } from './authService';
import { httpClient } from './httpClient';

jest.mock('./httpClient', () => ({
    httpClient: { get: jest.fn(), post: jest.fn() }
}));

describe('authService', () => {
    beforeEach(() => jest.clearAllMocks());

    it('posts the credentials to sign-in without reporting a 401 as an expiry', async () => {
        const controller = new AbortController();
        (httpClient.post as jest.Mock).mockResolvedValue({ username: 'magasinier' });

        const session = await authService.signIn({ username: 'magasinier', password: 'Reception2026' }, controller.signal);

        expect(session).toEqual({ username: 'magasinier' });
        expect(httpClient.post).toHaveBeenCalledWith(
            '/identity/auth/sign-in',
            { username: 'magasinier', password: 'Reception2026' },
            { signal: controller.signal, sessionAware: false }
        );
    });

    it('posts to sign-out with no body', async () => {
        await authService.signOut();

        expect(httpClient.post).toHaveBeenCalledWith('/identity/auth/sign-out', undefined, { signal: undefined, sessionAware: false });
    });

    it('reads the current session without reporting a 401 as an expiry', async () => {
        const controller = new AbortController();

        await authService.currentSession(controller.signal);

        expect(httpClient.get).toHaveBeenCalledWith('/identity/auth/me', { signal: controller.signal, sessionAware: false });
    });
});

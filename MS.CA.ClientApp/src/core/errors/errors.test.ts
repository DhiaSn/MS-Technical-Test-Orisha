import { AppError } from './AppError';
import { describeError } from './describeError';
import { errorFromResponse } from './errorFromResponse';
import { normalizeError } from './normalizeError';

function problem(status: number, body: unknown, contentType = 'application/problem+json'): Response {
    return new Response(typeof body === 'string' ? body : JSON.stringify(body), { status, headers: { 'content-type': contentType } });
}

describe('errorFromResponse', () => {
    it('reads the stable code of a problem response', async () => {
        const error = await errorFromResponse(
            problem(404, { status: 404, title: 'Delivery not found.', code: 'reception.delivery_not_found' })
        );

        expect(error).toMatchObject({ kind: 'notFound', status: 404, code: 'reception.delivery_not_found' });
    });

    it('keeps per-field codes and their parameters for a validation problem', async () => {
        const error = await errorFromResponse(
            problem(400, {
                status: 400,
                code: 'validation.failed',
                errorCodes: { receivedQuantity: [{ code: 'reception.quantity_out_of_range', params: { min: '0', max: '50' } }] }
            })
        );

        expect(error.kind).toBe('validation');
        expect(error.fieldCodes?.receivedQuantity?.[0]).toEqual({
            code: 'reception.quantity_out_of_range',
            params: { min: '0', max: '50' }
        });
    });

    it('reads a 401 as an unauthorized error', async () => {
        const error = await errorFromResponse(problem(401, { status: 401, code: 'auth.unauthenticated' }));

        expect(error).toMatchObject({ kind: 'unauthorized', status: 401, code: 'auth.unauthenticated' });
    });

    it('survives a non-JSON error body such as a proxy 502 page', async () => {
        const error = await errorFromResponse(problem(502, '<html>Bad gateway</html>', 'text/html'));

        expect(error).toMatchObject({ kind: 'server', status: 502 });
        expect(error.code).toBeUndefined();
    });
});

describe('normalizeError', () => {
    it('maps a failed fetch to a network error', () => {
        expect(normalizeError(new TypeError('Failed to fetch')).kind).toBe('network');
    });

    it('maps a timeout abort to a timeout error', () => {
        expect(normalizeError(new DOMException('The operation timed out.', 'TimeoutError')).kind).toBe('timeout');
    });

    it('passes an AppError through unchanged', () => {
        const original = new AppError({ kind: 'notFound', message: 'x' });
        expect(normalizeError(original)).toBe(original);
    });

    it('turns anything else into an unknown error', () => {
        expect(normalizeError('boom').kind).toBe('unknown');
    });
});

describe('describeError', () => {
    it('uses the French message of a known code', () => {
        const error = new AppError({ kind: 'notFound', message: 'x', code: 'reception.delivery_not_found' });
        expect(describeError(error)).toBe('Cette commande est introuvable.');
    });

    it('interpolates the bounds of a field error', () => {
        const error = new AppError({
            kind: 'validation',
            message: 'x',
            code: 'validation.failed',
            fieldCodes: { receivedQuantity: [{ code: 'reception.quantity_out_of_range', params: { min: '0', max: '50' } }] }
        });

        expect(describeError(error, 'receivedQuantity')).toBe('La quantité reçue doit être comprise entre 0 et 50.');
    });

    it('falls back to a message for the kind when the code is unknown', () => {
        const error = new AppError({ kind: 'server', message: 'x', code: 'something.new' });
        expect(describeError(error)).toBe('Une erreur est survenue côté serveur. Réessayez dans un instant.');
    });

    it.each(['constructor', '__proto__', 'toString', 'hasOwnProperty'])(
        'treats the inherited property name %s as an unknown code',
        (code) => {
            const error = new AppError({ kind: 'server', message: 'x', code });
            expect(describeError(error)).toBe('Une erreur est survenue côté serveur. Réessayez dans un instant.');
        }
    );

    it.each([
        ['auth.invalid_credentials', 'Identifiant ou mot de passe incorrect.'],
        ['auth.unauthenticated', 'Votre session a expiré. Reconnectez-vous.'],
        ['rate_limit.exceeded', 'Trop de tentatives. Réessayez dans une minute.'],
        ['account.username_taken', 'Cet identifiant est déjà utilisé.'],
        ['csrf.header_missing', 'La requête a été refusée pour des raisons de sécurité. Rechargez la page.']
    ])('%s has a French message', (code, message) => {
        expect(describeError(new AppError({ kind: 'unknown', message: 'x', code }))).toBe(message);
    });

    it('never shows the raw diagnostic message', () => {
        const error = new AppError({ kind: 'unknown', message: 'NullReferenceException at Foo.Bar' });
        expect(describeError(error)).not.toContain('NullReference');
    });
});

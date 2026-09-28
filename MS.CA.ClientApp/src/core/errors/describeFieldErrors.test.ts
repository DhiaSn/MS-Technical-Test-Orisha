import { AppError } from './AppError';
import { describeFieldErrors } from './describeFieldErrors';

describe('describeFieldErrors', () => {
    it('describes every code recorded for the field', () => {
        const error = new AppError({
            kind: 'validation',
            message: 'x',
            code: 'validation.failed',
            fieldCodes: {
                password: [{ code: 'password.too_short', params: { min: '8' } }, { code: 'password.missing_uppercase' }]
            }
        });

        expect(describeFieldErrors(error, 'password')).toEqual([
            'Le mot de passe doit contenir au moins 8 caractères.',
            'Le mot de passe doit contenir au moins une majuscule.'
        ]);
    });

    it('returns an empty array when the field has no errors', () => {
        const error = new AppError({ kind: 'validation', message: 'x', code: 'validation.failed' });

        expect(describeFieldErrors(error, 'password')).toEqual([]);
    });
});

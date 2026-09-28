import type { PasswordPolicy } from '@/features/auth/types';
import { buildPasswordHint } from './passwordHint';

describe('buildPasswordHint', () => {
    it('lists every required rule, joined with commas and a final "et"', () => {
        const policy: PasswordPolicy = { minimumLength: 8, requireUppercase: true, requireLowercase: true, requireDigit: true };

        expect(buildPasswordHint(policy)).toBe('Au moins 8 caractères, dont une majuscule, une minuscule et un chiffre.');
    });

    it('omits a rule the server does not require', () => {
        const policy: PasswordPolicy = { minimumLength: 6, requireUppercase: false, requireLowercase: true, requireDigit: false };

        expect(buildPasswordHint(policy)).toBe('Au moins 6 caractères, dont une minuscule.');
    });

    it('states only the length when nothing else is required', () => {
        const policy: PasswordPolicy = { minimumLength: 6, requireUppercase: false, requireLowercase: false, requireDigit: false };

        expect(buildPasswordHint(policy)).toBe('Au moins 6 caractères.');
    });

    it('joins exactly two rules with "et" and no comma', () => {
        const policy: PasswordPolicy = { minimumLength: 8, requireUppercase: true, requireLowercase: false, requireDigit: true };

        expect(buildPasswordHint(policy)).toBe('Au moins 8 caractères, dont une majuscule et un chiffre.');
    });
});

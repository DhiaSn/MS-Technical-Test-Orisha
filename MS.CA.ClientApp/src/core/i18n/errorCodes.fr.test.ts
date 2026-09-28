import { errorCodesFr } from './messages/errorCodes.fr';

const CONTRACT_CODES = [
    'validation.failed',
    'request.invalid',
    'request.unsupported_media_type',
    'resource.not_found',
    'reception.delivery_not_found',
    'reception.pallet_not_found',
    'reception.carton_not_found',
    'reception.product_not_found',
    'field.required',
    'field.too_long',
    'reception.quantity_out_of_range',
    'reception.expected_quantity_not_positive',
    'state.invalid',
    'server.error',
    'auth.unauthenticated',
    'auth.invalid_credentials',
    'access.forbidden',
    'csrf.header_missing',
    'csrf.origin_not_allowed',
    'rate_limit.exceeded',
    'resource.conflict',
    'account.username_taken',
    'username.invalid',
    'password.required',
    'password.too_short',
    'password.missing_uppercase',
    'password.missing_lowercase',
    'password.missing_digit'
];

describe('errorCodesFr', () => {
    it.each(CONTRACT_CODES)('has a French message for %s', (code) => {
        expect(errorCodesFr[code]).toEqual(expect.any(String));
        expect(errorCodesFr[code]).not.toBe('');
    });
});

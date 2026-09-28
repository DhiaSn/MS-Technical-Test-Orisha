import { interpolate, translate } from './translate';

describe('translate', () => {
    it('returns the message of a key', () => {
        expect(translate('action.retry')).toBe('Réessayer');
    });
});

describe('interpolate', () => {
    it('replaces the named placeholders', () => {
        expect(interpolate('Entre {min} et {max}.', { min: 0, max: 50 })).toBe('Entre 0 et 50.');
    });

    it('leaves an unknown placeholder untouched', () => {
        expect(interpolate('Entre {min} et {max}.', { min: 0 })).toBe('Entre 0 et {max}.');
    });

    it('leaves every placeholder untouched without parameters', () => {
        expect(interpolate('Entre {min} et {max}.')).toBe('Entre {min} et {max}.');
    });
});

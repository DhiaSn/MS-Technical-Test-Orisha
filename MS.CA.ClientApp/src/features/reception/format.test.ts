import { formatProgress, percentOf, statusLabel } from './format';

describe('formatProgress', () => {
    it('reads "received / expected"', () => {
        expect(formatProgress({ receivedUnits: 312, expectedUnits: 504 })).toBe('312 / 504');
    });
});

describe('percentOf', () => {
    it.each([
        [{ receivedUnits: 0, expectedUnits: 0 }, 0],
        [{ receivedUnits: 0, expectedUnits: 504 }, 0],
        [{ receivedUnits: 252, expectedUnits: 504 }, 50],
        [{ receivedUnits: 504, expectedUnits: 504 }, 100]
    ])('%j → %i %', (progress, expected) => {
        expect(percentOf(progress)).toBe(expected);
    });
});

describe('statusLabel', () => {
    it.each([
        ['none', 'Non reçu'],
        ['partial', 'Partiellement reçu'],
        ['all', 'Reçu']
    ] as const)('%s → %s', (status, label) => {
        expect(statusLabel(status)).toBe(label);
    });
});

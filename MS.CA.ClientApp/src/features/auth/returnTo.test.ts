import { safeReturnTo } from './returnTo';

describe('safeReturnTo', () => {
    it.each([
        ['/reception', '/reception'],
        ['/reception?tab=1', '/reception?tab=1'],
        ['/reception#top', '/reception#top']
    ])('keeps the local path %s', (input, expected) => {
        expect(safeReturnTo(input)).toBe(expected);
    });

    it.each([
        '//evil.example',
        '///evil.example',
        'https://evil.example',
        'http://evil.example/reception',
        'javascript:alert(1)',
        '/\evil.example',
        '\\evil.example',
        'reception',
        '',
        ' /reception',
        '/reception\nSet-Cookie: x=1',
        '/reception\t/x',
        '/login',
        '/login?returnTo=%2Freception',
        '/login#top',
        '/sign-up'
    ])('falls back to the reception page for %j', (input) => {
        expect(safeReturnTo(input)).toBe('/reception');
    });

    it.each([null, undefined])('falls back for %s', (input) => {
        expect(safeReturnTo(input)).toBe('/reception');
    });
});

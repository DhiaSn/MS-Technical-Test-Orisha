import { render } from '@testing-library/react';
import { usePageTitle } from './usePageTitle';

function Page({ title }: { title: string }) {
    usePageTitle(title);
    return null;
}

describe('usePageTitle', () => {
    it('sets the document title and follows changes', () => {
        const { rerender } = render(<Page title="Réception" />);
        expect(document.title).toBe('Réception');

        rerender(<Page title="Palette PAL-01" />);
        expect(document.title).toBe('Palette PAL-01');
    });
});

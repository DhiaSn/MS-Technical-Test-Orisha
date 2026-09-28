import { render, screen } from '@testing-library/react';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { ProgressBar } from './ProgressBar';

describe('ProgressBar', () => {
    it('exposes its value to assistive technology', () => {
        render(<ProgressBar value={312} max={504} label="Avancement" />);

        const bar = screen.getByRole('progressbar', { name: 'Avancement' });
        expect(bar).toHaveAttribute('aria-valuenow', '312');
        expect(bar).toHaveAttribute('aria-valuemax', '504');
        expect(bar).toHaveAttribute('aria-valuemin', '0');
    });

    it('fills in proportion to the value', () => {
        render(<ProgressBar value={1} max={4} label="Avancement" />);

        expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '25%' });
    });

    it('reads 0 % rather than NaN when nothing is expected', () => {
        render(<ProgressBar value={0} max={0} label="Avancement" />);

        expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '0%' });
    });

    it('has no accessibility violations', async () => {
        const { container } = render(<ProgressBar value={3} max={10} label="Avancement" />);

        await expectNoA11yViolations(container);
    });
});

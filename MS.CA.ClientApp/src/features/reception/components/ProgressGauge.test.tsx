import { render, screen } from '@testing-library/react';
import { ProgressGauge } from './ProgressGauge';

describe('ProgressGauge', () => {
    it('states received and expected articles', () => {
        render(<ProgressGauge progress={{ receivedUnits: 312, expectedUnits: 504 }} status="partial" />);

        expect(screen.getByText('312 / 504 articles reçus')).toBeInTheDocument();
        expect(screen.getByRole('progressbar', { name: 'Avancement de la réception' })).toHaveAttribute('aria-valuenow', '312');
    });

    it('announces completion when everything is received', () => {
        render(<ProgressGauge progress={{ receivedUnits: 504, expectedUnits: 504 }} status="all" />);

        expect(screen.getByText('Réception terminée')).toBeInTheDocument();
    });

    it('shows 0 / 0 without breaking for an empty delivery', () => {
        render(<ProgressGauge progress={{ receivedUnits: 0, expectedUnits: 0 }} status="none" />);

        expect(screen.getByText('0 / 0 articles reçus')).toBeInTheDocument();
    });
});

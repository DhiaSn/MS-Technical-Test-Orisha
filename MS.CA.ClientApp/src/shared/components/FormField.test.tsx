import { render, screen } from '@testing-library/react';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { FormField } from './FormField';

describe('FormField', () => {
    it('binds the label to its control', () => {
        render(
            <FormField label="Adresse e-mail">
                <input type="email" />
            </FormField>
        );

        expect(screen.getByLabelText('Adresse e-mail')).toBeInTheDocument();
    });

    it('marks the control invalid and describes it with the error', () => {
        render(
            <FormField label="Adresse e-mail" error="Adresse invalide.">
                <input type="email" />
            </FormField>
        );

        const control = screen.getByLabelText('Adresse e-mail');
        const message = screen.getByRole('alert');
        expect(message).toHaveTextContent('Adresse invalide.');
        expect(control).toHaveAttribute('aria-invalid', 'true');
        expect(control).toHaveAccessibleDescription('Adresse invalide.');
        expect(control.getAttribute('aria-describedby')).toBe(message.id);
    });

    it('is neither invalid nor described without an error', () => {
        render(
            <FormField label="Adresse e-mail">
                <input type="email" />
            </FormField>
        );

        const control = screen.getByLabelText('Adresse e-mail');
        expect(control).not.toHaveAttribute('aria-invalid');
        expect(control).not.toHaveAttribute('aria-describedby');
        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('gives two fields distinct ids', () => {
        render(
            <>
                <FormField label="A">
                    <input />
                </FormField>
                <FormField label="B">
                    <input />
                </FormField>
            </>
        );

        expect(screen.getByLabelText('A').id).not.toBe(screen.getByLabelText('B').id);
    });

    it('has no accessibility violations, with or without an error', async () => {
        const { container } = render(
            <>
                <FormField label="A">
                    <input />
                </FormField>
                <FormField label="B" error="Champ requis.">
                    <input />
                </FormField>
            </>
        );

        await expectNoA11yViolations(container);
    });
});

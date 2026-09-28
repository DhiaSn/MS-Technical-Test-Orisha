import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { TriStateCheckbox } from './TriStateCheckbox';

describe('TriStateCheckbox', () => {
    it.each([
        ['none', false, false],
        ['partial', false, true],
        ['all', true, false]
    ] as const)('renders %s as checked=%s indeterminate=%s', (status, checked, indeterminate) => {
        render(<TriStateCheckbox status={status} label="Palette PAL-01" onChange={jest.fn()} />);

        const box = screen.getByRole('checkbox', { name: 'Palette PAL-01' }) as HTMLInputElement;
        expect(box.checked).toBe(checked);
        expect(box.indeterminate).toBe(indeterminate);
    });

    it('reports a partial status as mixed to assistive technology', () => {
        render(<TriStateCheckbox status="partial" label="x" onChange={jest.fn()} />);

        expect(screen.getByRole('checkbox')).toBePartiallyChecked();
    });

    it.each([
        ['none', true],
        ['partial', true],
        ['all', false]
    ] as const)('from %s asks for validated=%s when clicked', async (status, expected) => {
        const onChange = jest.fn();
        render(<TriStateCheckbox status={status} label="x" onChange={onChange} />);

        await userEvent.click(screen.getByRole('checkbox'));

        expect(onChange).toHaveBeenCalledWith(expected);
    });

    it('can be toggled from the keyboard', async () => {
        const onChange = jest.fn();
        render(<TriStateCheckbox status="none" label="x" onChange={onChange} />);

        await userEvent.tab();
        await userEvent.keyboard(' ');

        expect(onChange).toHaveBeenCalledWith(true);
    });

    it('does not fire while disabled', async () => {
        const onChange = jest.fn();
        render(<TriStateCheckbox status="none" label="x" onChange={onChange} disabled />);

        await userEvent.click(screen.getByRole('checkbox'));

        expect(onChange).not.toHaveBeenCalled();
    });

    it('does not fire while busy, and says so', async () => {
        const onChange = jest.fn();
        render(<TriStateCheckbox status="none" label="x" onChange={onChange} busy />);

        await userEvent.click(screen.getByRole('checkbox'));

        expect(onChange).not.toHaveBeenCalled();
        expect(screen.getByRole('checkbox')).toHaveAttribute('aria-busy', 'true');
        expect(screen.getByRole('checkbox')).toHaveAttribute('aria-disabled', 'true');
    });

    it('keeps keyboard focus when it becomes busy and ignores the keyboard while busy', async () => {
        const onChange = jest.fn();
        const { rerender } = render(<TriStateCheckbox status="none" label="x" onChange={onChange} />);
        await userEvent.tab();
        expect(screen.getByRole('checkbox')).toHaveFocus();

        rerender(<TriStateCheckbox status="none" label="x" onChange={onChange} busy />);
        await userEvent.keyboard(' ');

        expect(screen.getByRole('checkbox')).toBeEnabled();
        expect(screen.getByRole('checkbox')).toHaveFocus();
        expect(onChange).not.toHaveBeenCalled();
    });

    it('does not change its own state: it shows what it is given', async () => {
        render(<TriStateCheckbox status="none" label="x" onChange={jest.fn()} />);

        await userEvent.click(screen.getByRole('checkbox'));

        expect(screen.getByRole('checkbox')).not.toBeChecked();
    });

    it('follows the status it is given when it changes', () => {
        const { rerender } = render(<TriStateCheckbox status="partial" label="x" onChange={jest.fn()} />);
        rerender(<TriStateCheckbox status="all" label="x" onChange={jest.fn()} />);

        const box = screen.getByRole('checkbox') as HTMLInputElement;
        expect(box.checked).toBe(true);
        expect(box.indeterminate).toBe(false);
    });

    it('has no accessibility violations in any state', async () => {
        const { container } = render(
            <>
                <TriStateCheckbox status="none" label="A" onChange={jest.fn()} />
                <TriStateCheckbox status="partial" label="B" onChange={jest.fn()} />
                <TriStateCheckbox status="all" label="C" onChange={jest.fn()} />
            </>
        );

        await expectNoA11yViolations(container);
    });
});

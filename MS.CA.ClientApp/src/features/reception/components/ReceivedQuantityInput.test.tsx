import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppError } from '@/core/errors';
import { ReceivedQuantityInput } from './ReceivedQuantityInput';

const setup = (onCommit = jest.fn().mockResolvedValue(undefined)) => {
    render(<ReceivedQuantityInput reference="TSH-RED-M" expected={50} received={10} disabled={false} onCommit={onCommit} />);
    return onCommit;
};

describe('ReceivedQuantityInput', () => {
    it('commits a changed integer on Enter', async () => {
        const onCommit = setup();
        const input = screen.getByRole('spinbutton', { name: 'Quantité reçue pour TSH-RED-M' });

        await userEvent.clear(input);
        await userEvent.type(input, '25{Enter}');

        expect(onCommit).toHaveBeenCalledWith(25);
    });

    it('commits on blur', async () => {
        const onCommit = setup();
        const input = screen.getByRole('spinbutton');

        await userEvent.clear(input);
        await userEvent.type(input, '7');
        await userEvent.tab();

        expect(onCommit).toHaveBeenCalledWith(7);
    });

    it('does not call the server when nothing changed', async () => {
        const onCommit = setup();

        await userEvent.click(screen.getByRole('spinbutton'));
        await userEvent.tab();

        expect(onCommit).not.toHaveBeenCalled();
    });

    it.each(['', '3.5'])('rejects "%s" locally with a French message and does not call the server', async (typed) => {
        const onCommit = setup();
        const input = screen.getByRole('spinbutton');
        await userEvent.clear(input);
        if (typed) await userEvent.type(input, typed);
        await userEvent.tab();

        expect(onCommit).not.toHaveBeenCalled();
        expect(screen.getByRole('alert')).toHaveTextContent('Saisissez un nombre entier.');
    });

    it("shows the server's bounds error next to the field and restores the displayed value", async () => {
        const error = new AppError({
            kind: 'validation',
            message: 'x',
            code: 'validation.failed',
            fieldCodes: { receivedQuantity: [{ code: 'reception.quantity_out_of_range', params: { min: '0', max: '50' } }] }
        });
        setup(jest.fn().mockResolvedValue(error));
        const input = screen.getByRole('spinbutton');

        await userEvent.clear(input);
        await userEvent.type(input, '999{Enter}');

        expect(await screen.findByRole('alert')).toHaveTextContent('La quantité reçue doit être comprise entre 0 et 50.');
    });

    it('clears a showing error once an external update changes the received quantity', async () => {
        const error = new AppError({
            kind: 'validation',
            message: 'x',
            code: 'validation.failed',
            fieldCodes: { receivedQuantity: [{ code: 'reception.quantity_out_of_range', params: { min: '0', max: '50' } }] }
        });
        const { rerender } = render(
            <ReceivedQuantityInput
                reference="TSH-RED-M"
                expected={50}
                received={10}
                disabled={false}
                onCommit={jest.fn().mockResolvedValue(error)}
            />
        );
        const input = screen.getByRole('spinbutton');
        await userEvent.clear(input);
        await userEvent.type(input, '999{Enter}');
        expect(await screen.findByRole('alert')).toBeInTheDocument();

        rerender(
            <ReceivedQuantityInput
                reference="TSH-RED-M"
                expected={50}
                received={30}
                disabled={false}
                onCommit={jest.fn().mockResolvedValue(undefined)}
            />
        );

        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
        expect(screen.getByRole('spinbutton')).toHaveValue(30);
    });

    it('follows the received quantity when the server changes it', () => {
        const { rerender } = render(
            <ReceivedQuantityInput reference="R" expected={50} received={10} disabled={false} onCommit={jest.fn()} />
        );

        rerender(<ReceivedQuantityInput reference="R" expected={50} received={50} disabled={false} onCommit={jest.fn()} />);

        expect(screen.getByRole('spinbutton')).toHaveValue(50);
    });
});

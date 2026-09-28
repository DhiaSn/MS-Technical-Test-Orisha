import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { cartonAId, deliveryFixture, palletId, productId } from '@/features/reception/testing/deliveryFixture';
import { DeliveryTree, type DeliveryTreeProps } from './DeliveryTree';

const setup = (delivery = deliveryFixture(), overrides: Partial<DeliveryTreeProps> = {}) => {
    const props: DeliveryTreeProps = {
        delivery,
        isPending: () => false,
        onTogglePallet: jest.fn(),
        onToggleCarton: jest.fn(),
        onToggleProduct: jest.fn(),
        onSetQuantity: jest.fn().mockResolvedValue(undefined),
        ...overrides
    };
    return { props, ...render(<DeliveryTree {...props} />) };
};

describe('DeliveryTree', () => {
    it('shows pallets collapsed: no carton or product is in the DOM yet', () => {
        setup();

        expect(screen.getByText('Palette PAL-01')).toBeInTheDocument();
        expect(screen.queryByText('Carton CART-01-A')).not.toBeInTheDocument();
        expect(screen.queryByText('TSH-RED-M')).not.toBeInTheDocument();
    });

    it('reveals cartons then products level by level', async () => {
        setup();

        await userEvent.click(screen.getByRole('button', { name: 'Déplier Palette PAL-01' }));
        expect(screen.getByText('Carton CART-01-A')).toBeInTheDocument();
        expect(screen.queryByText('TSH-RED-M')).not.toBeInTheDocument();

        await userEvent.click(screen.getByRole('button', { name: 'Déplier Carton CART-01-A' }));
        expect(screen.getByText('TSH-RED-M')).toBeInTheDocument();
    });

    it('marks the disclosure buttons with aria-expanded', async () => {
        setup();
        const toggle = screen.getByRole('button', { name: 'Déplier Palette PAL-01' });
        expect(toggle).toHaveAttribute('aria-expanded', 'false');

        await userEvent.click(toggle);

        expect(screen.getByRole('button', { name: 'Replier Palette PAL-01' })).toHaveAttribute('aria-expanded', 'true');
    });

    it("renders each level's status exactly as the server sent it", async () => {
        const delivery = deliveryFixture({ palletStatus: 'partial', cartonStatuses: ['all', 'none'] });
        setup(delivery);

        expect(screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' })).toBePartiallyChecked();
        await userEvent.click(screen.getByRole('button', { name: 'Déplier Palette PAL-01' }));
        expect(screen.getByRole('checkbox', { name: 'Valider le carton CART-01-A' })).toBeChecked();
        expect(screen.getByRole('checkbox', { name: 'Valider le carton CART-01-B' })).not.toBeChecked();
    });

    it('asks to validate a pallet, a carton and a product with the id of the clicked node', async () => {
        const { props } = setup();
        await userEvent.click(screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' }));
        expect(props.onTogglePallet).toHaveBeenCalledWith(palletId, true);

        await userEvent.click(screen.getByRole('button', { name: 'Déplier Palette PAL-01' }));
        await userEvent.click(screen.getByRole('checkbox', { name: 'Valider le carton CART-01-A' }));
        expect(props.onToggleCarton).toHaveBeenCalledWith(cartonAId, true);

        await userEvent.click(screen.getByRole('button', { name: 'Déplier Carton CART-01-A' }));
        await userEvent.click(screen.getByRole('checkbox', { name: 'Valider le produit TSH-RED-M' }));
        expect(props.onToggleProduct).toHaveBeenCalledWith(productId, true);
    });

    it('asks to un-validate a fully received node', async () => {
        const { props } = setup(deliveryFixture({ palletStatus: 'all' }));

        await userEvent.click(screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' }));

        expect(props.onTogglePallet).toHaveBeenCalledWith(palletId, false);
    });

    it('keeps nodes expanded when the delivery is updated', async () => {
        const { rerender, props } = setup();
        await userEvent.click(screen.getByRole('button', { name: 'Déplier Palette PAL-01' }));

        rerender(<DeliveryTree {...props} delivery={deliveryFixture({ palletStatus: 'partial' })} />);

        expect(screen.getByText('Carton CART-01-A')).toBeInTheDocument();
    });

    it('marks the checkbox of a node whose request is in flight as busy, keeps it focusable and ignores a click on it', async () => {
        const { props } = setup(deliveryFixture(), { isPending: (id) => id === palletId });
        const checkbox = screen.getByRole('checkbox', { name: 'Valider la palette PAL-01' });

        expect(checkbox).toHaveAttribute('aria-busy', 'true');
        expect(checkbox).toBeEnabled();

        await userEvent.click(checkbox);

        expect(props.onTogglePallet).not.toHaveBeenCalled();
    });

    it('shows the received/expected units of the pallet from the payload', () => {
        setup(deliveryFixture({ received: 60 }));

        expect(screen.getByText('60 / 180')).toBeInTheDocument();
    });
});

import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useTheme } from 'next-themes';
import { ThemeToggle } from './ThemeToggle';

jest.mock('next-themes', () => ({ useTheme: jest.fn() }));

const mockedUseTheme = useTheme as jest.Mock;

describe('ThemeToggle', () => {
    it('labels itself in French from the resolved (system-aware) theme, not the raw preference', () => {
        mockedUseTheme.mockReturnValue({ theme: 'system', resolvedTheme: 'dark', setTheme: jest.fn() });
        render(<ThemeToggle />);

        expect(screen.getByRole('button', { name: 'Activer le thème clair' })).toBeInTheDocument();
    });

    it('switches to the other theme on click', async () => {
        const setTheme = jest.fn();
        mockedUseTheme.mockReturnValue({ theme: 'light', resolvedTheme: 'light', setTheme });
        render(<ThemeToggle />);

        await userEvent.click(screen.getByRole('button', { name: 'Activer le thème sombre' }));

        expect(setTheme).toHaveBeenCalledWith('dark');
    });
});

import { renderHook, waitFor } from '@testing-library/react';
import { authService } from '@/api';
import { createTestClient, TestProviders } from '@/shared/testing/renderWithProviders';
import { usePasswordPolicy } from './usePasswordPolicy';

jest.mock('@/api', () => ({ authService: { passwordPolicy: jest.fn() } }));

const mockedPasswordPolicy = authService.passwordPolicy as jest.Mock;

describe('usePasswordPolicy', () => {
    beforeEach(() => jest.resetAllMocks());

    it('fetches the password policy from the server', async () => {
        mockedPasswordPolicy.mockResolvedValue({
            minimumLength: 8,
            requireUppercase: true,
            requireLowercase: true,
            requireDigit: true
        });
        const client = createTestClient();
        const { result } = renderHook(() => usePasswordPolicy(), {
            wrapper: ({ children }) => <TestProviders client={client}>{children}</TestProviders>
        });

        await waitFor(() =>
            expect(result.current.data).toEqual({
                minimumLength: 8,
                requireUppercase: true,
                requireLowercase: true,
                requireDigit: true
            })
        );
    });
});

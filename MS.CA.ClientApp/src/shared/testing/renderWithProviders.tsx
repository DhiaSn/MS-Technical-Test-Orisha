import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import type { ReactElement, ReactNode } from 'react';
import { ToastProvider } from '@/shared/components';

export function createTestClient(): QueryClient {
    return new QueryClient({
        defaultOptions: {
            queries: { retry: false, gcTime: Infinity },
            mutations: { retry: false }
        }
    });
}

export function TestProviders({ client, children }: { client: QueryClient; children: ReactNode }) {
    return (
        <QueryClientProvider client={client}>
            <ToastProvider>{children}</ToastProvider>
        </QueryClientProvider>
    );
}

export function renderWithProviders(
    ui: ReactElement,
    { client = createTestClient(), ...options }: { client?: QueryClient } & Omit<RenderOptions, 'wrapper'> = {}
): RenderResult & { client: QueryClient } {
    const result = render(ui, {
        wrapper: ({ children }) => <TestProviders client={client}>{children}</TestProviders>,
        ...options
    });
    return { ...result, client };
}

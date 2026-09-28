'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { SessionProvider } from '@/features/auth/context/SessionContext';
import { ToastProvider } from '@/shared/components';

export function Providers({ children }: { children: React.ReactNode }) {
    const [queryClient] = useState(
        () =>
            new QueryClient({
                defaultOptions: {
                    queries: { retry: 1, refetchOnWindowFocus: true },
                    mutations: { retry: 0 }
                }
            })
    );

    return (
        <QueryClientProvider client={queryClient}>
            <ToastProvider>
                <SessionProvider>{children}</SessionProvider>
            </ToastProvider>
        </QueryClientProvider>
    );
}

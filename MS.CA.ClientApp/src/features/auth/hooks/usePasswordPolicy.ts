'use client';

import { useQuery } from '@tanstack/react-query';
import { authService } from '@/api';

export function usePasswordPolicy() {
    return useQuery({
        queryKey: ['auth', 'password-policy'],
        queryFn: ({ signal }) => authService.passwordPolicy(signal),
        staleTime: Infinity,
        retry: false
    });
}

export const env = {
    apiBasePath: process.env.NEXT_PUBLIC_API_BASE_PATH ?? '/api/core',
    isDevelopment: process.env.NODE_ENV === 'development'
} as const;

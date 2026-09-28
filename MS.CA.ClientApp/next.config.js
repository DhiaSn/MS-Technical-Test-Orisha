/** @type {import('next').NextConfig} */
const nextConfig = {
    reactStrictMode: true,
    output: 'standalone',
    sassOptions: { includePaths: ['./src/styles'] },
    async rewrites() {
        // Baked in at build time. In Docker the URL is a build argument (phase 13).
        if (process.env.API_PROXY_DISABLED === 'true') {
            return [];
        }

        const apiUrl = process.env.API_URL;
        if (!apiUrl) {
            throw new Error('API_URL is not set. Copy .env.example to .env.local, or set API_PROXY_DISABLED=true.');
        }

        return [{ source: '/api/core/:path*', destination: `${apiUrl.replace(/\/+$/, '')}/api/:path*` }];
    }
};

module.exports = nextConfig;

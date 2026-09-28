import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import { Providers } from '@/app/providers';
import { translate } from '@/core/i18n/translate';
import '@/styles/globals.scss';

const inter = Inter({ subsets: ['latin'], display: 'swap' });

export const metadata: Metadata = {
    title: translate('app.title'),
    description: translate('app.description')
};

export const viewport: Viewport = {
    width: 'device-width',
    initialScale: 1
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="fr" suppressHydrationWarning className={inter.className}>
            <body suppressHydrationWarning>
                <Providers>{children}</Providers>
            </body>
        </html>
    );
}

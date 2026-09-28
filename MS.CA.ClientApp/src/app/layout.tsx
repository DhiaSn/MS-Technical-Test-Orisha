import type { Metadata, Viewport } from 'next';
import { Providers } from '@/app/providers';
import { translate } from '@/core/i18n/translate';
import '@/styles/globals.scss';

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
        <html lang="fr">
            <body suppressHydrationWarning>
                <Providers>{children}</Providers>
            </body>
        </html>
    );
}

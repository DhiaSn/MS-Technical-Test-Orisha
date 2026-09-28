import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { translate } from '@/core/i18n/translate';
import { sessionFixture } from '@/features/auth/testing/sessionFixture';
import { createSessionStub, StubSessionProvider } from '@/features/auth/testing/stubSession';
import { ToastProvider } from '@/shared/components';
import { expectNoA11yViolations } from '@/shared/testing/axe';
import { AppShell } from './AppShell';

function renderShell() {
    return render(
        <ToastProvider>
            <StubSessionProvider value={createSessionStub({ status: 'signedIn', session: sessionFixture() })}>
                <AppShell>
                    <h1>Contenu</h1>
                </AppShell>
            </StubSessionProvider>
        </ToastProvider>
    );
}

describe('AppShell', () => {
    it('puts the skip link first and points it at the main content', async () => {
        renderShell();

        await userEvent.tab();

        const skip = screen.getByRole('link', { name: translate('skip.toContent') });
        expect(skip).toHaveFocus();
        expect(skip).toHaveAttribute('href', '#main');
        expect(screen.getByRole('main')).toHaveAttribute('id', 'main');
    });

    it('lays out a banner and the main content without accessibility violations', async () => {
        const { container } = renderShell();

        expect(screen.getByRole('banner')).toHaveTextContent(translate('app.title'));
        expect(screen.getByRole('main')).toHaveTextContent('Contenu');
        await expectNoA11yViolations(container);
    });
});

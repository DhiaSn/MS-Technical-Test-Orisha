import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

export async function expectNoA11yViolations(container: HTMLElement): Promise<void> {
    expect(await axe(container)).toHaveNoViolations();
}

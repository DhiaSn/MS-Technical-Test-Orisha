import { translate } from '@/core/i18n/translate';
import type { PasswordPolicy } from '@/features/auth/types';

function joinFr(items: readonly string[]): string {
    if (items.length <= 1) return items.join('');
    return `${items.slice(0, -1).join(', ')} et ${items[items.length - 1]}`;
}

export function buildPasswordHint(policy: PasswordPolicy): string {
    const rules = [
        policy.requireUppercase ? translate('auth.password.hint.uppercase') : undefined,
        policy.requireLowercase ? translate('auth.password.hint.lowercase') : undefined,
        policy.requireDigit ? translate('auth.password.hint.digit') : undefined
    ].filter((rule): rule is string => rule !== undefined);

    const length = translate('auth.password.hint.length', { min: policy.minimumLength });
    if (rules.length === 0) return `${length}.`;

    return `${length}, dont ${joinFr(rules)}.`;
}

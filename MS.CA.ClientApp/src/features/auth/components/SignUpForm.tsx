'use client';

import { useRef, useState } from 'react';
import { type AppError, describeError, describeFieldErrors, normalizeError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import { usePasswordPolicy } from '@/features/auth/hooks/usePasswordPolicy';
import { buildPasswordHint } from '@/features/auth/passwordHint';
import type { Registration } from '@/features/auth/types';
import { Alert, Button, FormField } from '@/shared/components';
import styles from './SignUpForm.module.scss';

export interface SignUpFormProps {
    onSubmit: (registration: Registration) => Promise<void>;
}

interface FieldErrors {
    username?: string;
    displayName?: string;
    password: string[];
}

const NO_FIELD_ERRORS: FieldErrors = { password: [] };

function fieldErrorsFor(error: AppError): FieldErrors {
    // A 409 carries no per-field codes: map the top-level conflict to the username field by hand.
    if (error.code === 'account.username_taken') {
        return { username: describeError(error), password: [] };
    }

    return {
        username: describeFieldErrors(error, 'username')[0],
        displayName: describeFieldErrors(error, 'displayName')[0],
        password: describeFieldErrors(error, 'password')
    };
}

export function SignUpForm({ onSubmit }: SignUpFormProps) {
    const passwordPolicy = usePasswordPolicy();
    const [username, setUsername] = useState('');
    const [displayName, setDisplayName] = useState('');
    const [password, setPassword] = useState('');
    const [missing, setMissing] = useState({ username: false, displayName: false, password: false });
    const [fieldErrors, setFieldErrors] = useState<FieldErrors>(NO_FIELD_ERRORS);
    const [message, setMessage] = useState<string>();
    const [pending, setPending] = useState(false);
    const passwordRef = useRef<HTMLInputElement>(null);
    // State updates are asynchronous: a second Enter can arrive before `pending` has re-rendered.
    const submitting = useRef(false);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (submitting.current) return;

        const next = { username: username.trim() === '', displayName: displayName.trim() === '', password: password === '' };
        setMissing(next);
        if (next.username || next.displayName || next.password) return;

        submitting.current = true;
        setPending(true);
        setMessage(undefined);
        setFieldErrors(NO_FIELD_ERRORS);
        try {
            await onSubmit({ username: username.trim(), displayName: displayName.trim(), password });
        } catch (cause) {
            const error = normalizeError(cause);
            const forFields = fieldErrorsFor(error);
            setFieldErrors(forFields);
            const hasFieldError = forFields.username !== undefined || forFields.displayName !== undefined || forFields.password.length > 0;
            if (!hasFieldError) setMessage(describeError(error));
            setPassword('');
            passwordRef.current?.focus();
            submitting.current = false;
            setPending(false);
        }
    };

    return (
        <form className={styles.form} onSubmit={submit} noValidate>
            <FormField
                label={translate('auth.username')}
                error={missing.username ? translate('auth.field.required') : fieldErrors.username}
            >
                <input
                    name="username"
                    autoComplete="username"
                    autoCapitalize="none"
                    spellCheck={false}
                    value={username}
                    onChange={(event) => setUsername(event.target.value)}
                />
            </FormField>
            <FormField
                label={translate('auth.displayName')}
                error={missing.displayName ? translate('auth.field.required') : fieldErrors.displayName}
            >
                <input
                    name="displayName"
                    autoComplete="name"
                    value={displayName}
                    onChange={(event) => setDisplayName(event.target.value)}
                />
            </FormField>
            <FormField label={translate('auth.password')} error={missing.password ? translate('auth.field.required') : undefined}>
                <input
                    ref={passwordRef}
                    name="password"
                    type="password"
                    autoComplete="new-password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                />
            </FormField>
            {fieldErrors.password.map((error) => (
                <p key={error} className={styles.error} role="alert">
                    {error}
                </p>
            ))}
            {passwordPolicy.data !== undefined && <p className={styles.hint}>{buildPasswordHint(passwordPolicy.data)}</p>}
            {message !== undefined && <Alert tone="error">{message}</Alert>}
            <Button type="submit" variant="primary" disabled={pending}>
                {translate(pending ? 'auth.signUp.submitting' : 'auth.signUp.submit')}
            </Button>
        </form>
    );
}

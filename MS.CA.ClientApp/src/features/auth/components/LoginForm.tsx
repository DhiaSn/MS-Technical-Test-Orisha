'use client';

import { useRef, useState } from 'react';
import { describeError, normalizeError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import type { Credentials } from '@/features/auth/types';
import { Alert, Button, FormField } from '@/shared/components';
import styles from './LoginForm.module.scss';

export interface LoginFormProps {
    onSubmit: (credentials: Credentials) => Promise<void>;
}

export function LoginForm({ onSubmit }: LoginFormProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [missing, setMissing] = useState({ username: false, password: false });
    const [message, setMessage] = useState<string>();
    const [pending, setPending] = useState(false);
    const passwordRef = useRef<HTMLInputElement>(null);
    // State updates are asynchronous: a second Enter can arrive before `pending` has re-rendered.
    const submitting = useRef(false);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (submitting.current) return;

        const next = { username: username.trim() === '', password: password === '' };
        setMissing(next);
        if (next.username || next.password) return;

        submitting.current = true;
        setPending(true);
        setMessage(undefined);
        try {
            await onSubmit({ username: username.trim(), password });
        } catch (cause) {
            setMessage(describeError(normalizeError(cause)));
            setPassword('');
            passwordRef.current?.focus();
            submitting.current = false;
            setPending(false);
        }
    };

    return (
        <form className={styles.form} onSubmit={submit} noValidate>
            <FormField label={translate('auth.username')} error={missing.username ? translate('auth.field.required') : undefined}>
                <input
                    name="username"
                    autoComplete="username"
                    autoCapitalize="none"
                    spellCheck={false}
                    value={username}
                    onChange={(event) => setUsername(event.target.value)}
                />
            </FormField>
            <FormField label={translate('auth.password')} error={missing.password ? translate('auth.field.required') : undefined}>
                <input
                    ref={passwordRef}
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                />
            </FormField>
            {message !== undefined && <Alert tone="error">{message}</Alert>}
            <Button type="submit" variant="primary" disabled={pending}>
                {translate(pending ? 'auth.login.submitting' : 'auth.login.submit')}
            </Button>
        </form>
    );
}

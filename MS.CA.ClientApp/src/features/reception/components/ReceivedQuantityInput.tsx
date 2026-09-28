'use client';

import { useId, useState } from 'react';
import { describeError, type AppError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import styles from './ReceivedQuantityInput.module.scss';

export interface ReceivedQuantityInputProps {
    reference: string;
    expected: number;
    received: number;
    disabled: boolean;
    onCommit: (quantity: number) => Promise<AppError | undefined>;
}

export function ReceivedQuantityInput({ reference, expected, received, disabled, onCommit }: ReceivedQuantityInputProps) {
    const [draft, setDraft] = useState(String(received));
    const [message, setMessage] = useState<string>();
    const [trackedReceived, setTrackedReceived] = useState(received);
    const errorId = useId();

    // Mirrors the server's value without an effect: a render-time prop/state comparison, not a
    // commit-then-correct round trip, so a fresh received quantity is never shown stale.
    if (received !== trackedReceived) {
        setTrackedReceived(received);
        setDraft(String(received));
        setMessage(undefined);
    }

    const commit = async () => {
        const value = Number(draft);
        if (draft.trim() === '' || !Number.isInteger(value)) {
            setMessage(translate('quantity.notInteger'));
            setDraft(String(received));
            return;
        }
        if (value === received) {
            setMessage(undefined);
            return;
        }

        const error = await onCommit(value);
        setMessage(error === undefined ? undefined : describeError(error, 'receivedQuantity'));
        if (error !== undefined) setDraft(String(received));
    };

    return (
        <div className={styles.field}>
            <input
                type="number"
                inputMode="numeric"
                min={0}
                max={expected}
                step={1}
                className={styles.input}
                value={draft}
                disabled={disabled}
                aria-label={translate('quantity.label', { reference })}
                aria-invalid={message !== undefined}
                aria-describedby={message === undefined ? undefined : errorId}
                onChange={(event) => setDraft(event.target.value)}
                onBlur={() => void commit()}
                onKeyDown={(event) => {
                    if (event.key === 'Enter') event.currentTarget.blur();
                }}
            />
            <span className={styles.of}>{translate('quantity.of', { expected })}</span>
            {message !== undefined && (
                <p id={errorId} role="alert" className={styles.error}>
                    {message}
                </p>
            )}
        </div>
    );
}

'use client';

import { useEffect, useRef } from 'react';
import styles from './TriStateCheckbox.module.scss';

export type CheckState = 'none' | 'partial' | 'all';

export interface TriStateCheckboxProps {
    status: CheckState;
    label: string;
    /** Receives the state being asked for: `true` to validate, `false` to withdraw the validation. */
    onChange: (validated: boolean) => void;
    disabled?: boolean;
    busy?: boolean;
}

export function TriStateCheckbox({ status, label, onChange, disabled = false, busy = false }: TriStateCheckboxProps) {
    const ref = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (ref.current !== null) {
            ref.current.indeterminate = status === 'partial';
        }
    }, [status]);

    // A busy box stays enabled: disabling an input drops the keyboard focus that is on it.
    return (
        <input
            ref={ref}
            type="checkbox"
            className={styles.box}
            data-status={status}
            checked={status === 'all'}
            aria-label={label}
            aria-busy={busy}
            aria-disabled={busy || undefined}
            disabled={disabled}
            onChange={() => {
                if (!busy) onChange(status !== 'all');
            }}
        />
    );
}

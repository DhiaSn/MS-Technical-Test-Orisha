import { cloneElement, useId, type ReactElement } from 'react';
import styles from './FormField.module.scss';

interface ControlProps {
    id?: string;
    'aria-invalid'?: boolean;
    'aria-describedby'?: string;
}

export interface FormFieldProps {
    label: string;
    error?: string;
    children: ReactElement<ControlProps>;
}

export function FormField({ label, error, children }: FormFieldProps) {
    const id = useId();
    const errorId = `${id}-error`;

    return (
        <div className={styles.field}>
            <label className={styles.label} htmlFor={id}>
                {label}
            </label>
            {cloneElement(children, {
                id,
                'aria-invalid': error === undefined ? undefined : true,
                'aria-describedby': error === undefined ? undefined : errorId
            })}
            {error !== undefined && (
                <p className={styles.error} id={errorId} role="alert">
                    {error}
                </p>
            )}
        </div>
    );
}

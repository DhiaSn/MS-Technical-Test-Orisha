import type { ButtonHTMLAttributes } from 'react';
import { cx } from './cx';
import styles from './Button.module.scss';

export type ButtonVariant = 'primary' | 'secondary';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant;
}

export function Button({ variant = 'primary', type = 'button', className, ...rest }: ButtonProps) {
    return <button {...rest} type={type} className={cx(styles.button, styles[variant], className)} />;
}

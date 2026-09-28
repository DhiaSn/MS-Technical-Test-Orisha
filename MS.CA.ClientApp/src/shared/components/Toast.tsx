'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { translate } from '@/core/i18n/translate';
import { cx } from './cx';
import styles from './Toast.module.scss';

const DISMISS_AFTER_MS = 5000;

type ToastTone = 'error' | 'info';

interface ToastEntry {
    id: number;
    tone: ToastTone;
    message: string;
}

interface ToastApi {
    error: (message: string) => void;
    info: (message: string) => void;
}

const TONE_LABELS = { error: 'tone.error', info: 'tone.info' } as const;

const ToastContext = createContext<ToastApi | undefined>(undefined);

export function ToastProvider({ children }: { children: React.ReactNode }) {
    const [toasts, setToasts] = useState<readonly ToastEntry[]>([]);
    const nextId = useRef(1);

    const dismiss = useCallback((id: number) => setToasts((current) => current.filter((toast) => toast.id !== id)), []);

    const api = useMemo<ToastApi>(() => {
        const push = (tone: ToastTone, message: string) => {
            const id = nextId.current++;
            setToasts((current) => [...current, { id, tone, message }]);
        };

        return { error: (message) => push('error', message), info: (message) => push('info', message) };
    }, []);

    return (
        <ToastContext value={api}>
            {children}
            <div className={styles.region} role="status" aria-label={translate('toast.region')}>
                {toasts.map((toast) => (
                    <ToastItem key={toast.id} toast={toast} onDismiss={dismiss} />
                ))}
            </div>
        </ToastContext>
    );
}

function ToastItem({ toast, onDismiss }: { toast: ToastEntry; onDismiss: (id: number) => void }) {
    useEffect(() => {
        const timer = setTimeout(() => onDismiss(toast.id), DISMISS_AFTER_MS);
        return () => clearTimeout(timer);
    }, [toast.id, onDismiss]);

    return (
        <div className={cx(styles.toast, styles[toast.tone])}>
            <p className={styles.message}>
                <span className="visually-hidden">{`${translate(TONE_LABELS[toast.tone])} : `}</span>
                {toast.message}
            </p>
            <button type="button" className={styles.close} aria-label={translate('toast.dismiss')} onClick={() => onDismiss(toast.id)}>
                <span aria-hidden="true">×</span>
            </button>
        </div>
    );
}

export function useToast(): ToastApi {
    const api = useContext(ToastContext);
    if (api === undefined) {
        throw new Error('useToast must be used inside a ToastProvider.');
    }
    return api;
}

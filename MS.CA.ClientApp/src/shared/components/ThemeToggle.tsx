'use client';

import { useTheme } from 'next-themes';
import { Sun, Moon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { translate } from '@/core/i18n/translate';
import styles from './ThemeToggle.module.scss';

export function ThemeToggle() {
    const { resolvedTheme, setTheme } = useTheme();
    const [mounted, setMounted] = useState(false);
    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setMounted(true);
    }, []);

    if (!mounted) {
        return <div className={styles.placeholder} />;
    }

    const isDark = resolvedTheme === 'dark';
    const label = translate(isDark ? 'theme.toggle.toLight' : 'theme.toggle.toDark');

    return (
        <button className={styles.toggle} onClick={() => setTheme(isDark ? 'light' : 'dark')} title={label} aria-label={label}>
            {isDark ? <Moon size={20} /> : <Sun size={20} />}
        </button>
    );
}

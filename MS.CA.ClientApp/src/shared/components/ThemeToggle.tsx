'use client';

import { useTheme } from 'next-themes';
import { Sun, Moon } from 'lucide-react';
import { useEffect, useState } from 'react';
import styles from './ThemeToggle.module.scss';

export function ThemeToggle() {
    const { theme, setTheme } = useTheme();
    const [mounted, setMounted] = useState(false);
    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setMounted(true);
    }, []);

    if (!mounted) {
        return <div className={styles.placeholder} />;
    }

    return (
        <button
            className={styles.toggle}
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            title="Toggle theme"
            aria-label="Toggle theme"
        >
            {theme === 'dark' ? <Moon size={20} /> : <Sun size={20} />}
        </button>
    );
}

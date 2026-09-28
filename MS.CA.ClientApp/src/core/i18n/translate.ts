import { fr } from './messages/fr';

export type MessageKey = keyof typeof fr;

export function interpolate(template: string, params?: Readonly<Record<string, string | number>>): string {
    return template.replace(/\{(\w+)\}/g, (match, name: string) => String(params?.[name] ?? match));
}

export function translate(key: MessageKey, params?: Readonly<Record<string, string | number>>): string {
    return interpolate(fr[key], params);
}

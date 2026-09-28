import { env } from '@/config/env';
import { errorFromResponse, normalizeError } from '@/core/errors';
import { sessionEvents } from './sessionEvents';

export interface HttpClientOptions {
    baseUrl?: string;
    timeoutMs?: number;
}

export interface RequestOptions {
    signal?: AbortSignal;
    /**
     * A 401 normally means the session is over and is reported to the session store. Sign-in, sign-up,
     * sign-out and the session probe set this to false: for them a 401 is the answer, not an expiry.
     */
    sessionAware?: boolean;
}

// The API refuses a cookie-bearing write without this header. A custom header cannot be added by a
// page on another origin without a CORS preflight the API never grants, which is what stops forgery.
export const CSRF_HEADER = 'X-MS-CSRF';

const SAFE_METHODS: ReadonlySet<string> = new Set(['GET', 'HEAD', 'OPTIONS']);
const DEFAULT_TIMEOUT_MS = 15_000;

export class HttpClient {
    private readonly baseUrl: string;
    private readonly timeoutMs: number;

    constructor({ baseUrl = env.apiBasePath, timeoutMs = DEFAULT_TIMEOUT_MS }: HttpClientOptions = {}) {
        this.baseUrl = baseUrl.replace(/\/+$/, '');
        this.timeoutMs = timeoutMs;
    }

    get<T>(path: string, options?: RequestOptions): Promise<T> {
        return this.send<T>('GET', path, undefined, options);
    }

    post<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> {
        return this.send<T>('POST', path, body, options);
    }

    put<T>(path: string, body: unknown, options?: RequestOptions): Promise<T> {
        return this.send<T>('PUT', path, body, options);
    }

    private async send<T>(method: string, path: string, body: unknown, options: RequestOptions = {}): Promise<T> {
        const timeout = AbortSignal.timeout(this.timeoutMs);
        const signal = options.signal === undefined ? timeout : AbortSignal.any([timeout, options.signal]);

        const headers = new Headers();
        if (body !== undefined) headers.set('Content-Type', 'application/json');
        if (!SAFE_METHODS.has(method)) headers.set(CSRF_HEADER, '1');

        try {
            const response = await fetch(`${this.baseUrl}${path}`, {
                method,
                headers,
                body: body === undefined ? undefined : JSON.stringify(body),
                credentials: 'same-origin',
                signal
            });

            if (!response.ok) {
                throw await errorFromResponse(response);
            }

            return (response.status === 204 ? undefined : await response.json()) as T;
        } catch (cause) {
            const error = normalizeError(cause);
            if (error.kind === 'unauthorized' && options.sessionAware !== false) {
                sessionEvents.emitEnded();
            }
            throw error;
        }
    }
}

export const httpClient = new HttpClient();

import JSDOMEnvironment from 'jest-environment-jsdom';
import type { EnvironmentContext, JestEnvironmentConfig } from '@jest/environment';

// jsdom implements the DOM but not the WHATWG fetch stack, and the HTTP client is built on it.
// Node has supplied these globals since 18, so they are copied in rather than polyfilled.
const NODE_GLOBALS = [
    'fetch',
    'Request',
    'Response',
    'Headers',
    'FormData',
    'AbortController',
    'AbortSignal',
    'ReadableStream',
    'TransformStream',
    'structuredClone'
] as const;

export default class NextJestEnvironment extends JSDOMEnvironment {
    constructor(config: JestEnvironmentConfig, context: EnvironmentContext) {
        super(config, context);

        for (const name of NODE_GLOBALS) {
            if (this.global[name] === undefined) {
                Reflect.set(this.global, name, Reflect.get(globalThis, name));
            }
        }
    }
}

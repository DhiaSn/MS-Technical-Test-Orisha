import nextJest from 'next/jest.js';
import type { Config } from 'jest';

const createJestConfig = nextJest({ dir: './' });

const config: Config = {
    testEnvironment: '<rootDir>/jest.environment.ts',
    setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
    moduleNameMapper: {
        '^@/(.*)$': '<rootDir>/src/$1'
    },
    collectCoverageFrom: ['src/**/*.{ts,tsx}', '!src/**/*.test.{ts,tsx}', '!src/**/testing/**']
};

export default createJestConfig(config);

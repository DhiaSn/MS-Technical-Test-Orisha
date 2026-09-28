import coreWebVitals from 'eslint-config-next/core-web-vitals';
import typescriptConfig from 'eslint-config-next/typescript';

const config = [
    {
        ignores: ['.next/**', 'coverage/**', 'next-env.d.ts']
    },
    ...coreWebVitals,
    ...typescriptConfig,
    {
        rules: {
            '@typescript-eslint/no-explicit-any': 'error',
            '@typescript-eslint/consistent-type-imports': ['error', { prefer: 'type-imports', fixStyle: 'inline-type-imports' }],
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: ['../*'],
                            message: 'Import across folders through the @/ aliases rather than relative parent paths.'
                        }
                    ]
                }
            ]
        }
    }
];

export default config;

// The gate a change must pass before review: the same steps, in the same order, locally and in CI.
import { spawnSync } from 'node:child_process';

const STEPS = [
    ['typecheck', ['run', 'typecheck']],
    ['lint', ['run', 'lint']],
    ['format:check', ['run', 'format:check']],
    ['test', ['test', '--', '--ci']],
    ['build', ['run', 'build']]
];

// next.config.js refuses to build without a backend URL. A check run has no backend, so the dev
// proxy is switched off unless the caller already chose one.
const env = { ...process.env };
if (env.API_URL === undefined && env.API_PROXY_DISABLED === undefined) {
    env.API_PROXY_DISABLED = 'true';
}

const results = [];
for (const [name, args] of STEPS) {
    console.log(`\n> ${name}`);
    const started = Date.now();
    const { status, error, signal } = spawnSync('npm', args, { stdio: 'inherit', env, shell: true });
    if (error !== undefined || signal !== null) {
        console.error(`${name} did not run to completion: ${error?.message ?? `killed by ${signal}`}`);
    }
    results.push({ name, ok: status === 0, seconds: ((Date.now() - started) / 1000).toFixed(1) });
    if (status !== 0) {
        break;
    }
}

console.log('\nSummary');
for (const { name, ok, seconds } of results) {
    console.log(`  ${ok ? 'pass' : 'FAIL'}  ${name} (${seconds}s)`);
}
const skipped = STEPS.slice(results.length).map(([name]) => name);
if (skipped.length > 0) {
    console.log(`  skipped: ${skipped.join(', ')}`);
}
process.exit(results.every((result) => result.ok) && skipped.length === 0 ? 0 : 1);

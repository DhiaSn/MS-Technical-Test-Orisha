import { AppError, type AppErrorKind, type FieldErrorCode } from './AppError';

interface ProblemBody {
    code?: unknown;
    title?: unknown;
    errorCodes?: unknown;
}

function kindOf(status: number): AppErrorKind {
    if (status === 401) return 'unauthorized';
    if (status === 404) return 'notFound';
    if (status === 400 || status === 422) return 'validation';
    if (status >= 500) return 'server';
    return 'unknown';
}

async function readProblem(response: Response): Promise<ProblemBody> {
    try {
        const parsed: unknown = await response.json();
        return typeof parsed === 'object' && parsed !== null ? (parsed as ProblemBody) : {};
    } catch {
        return {};
    }
}

function readFieldCodes(value: unknown): Record<string, FieldErrorCode[]> | undefined {
    if (typeof value !== 'object' || value === null) return undefined;
    return value as Record<string, FieldErrorCode[]>;
}

export async function errorFromResponse(response: Response): Promise<AppError> {
    const problem = await readProblem(response);
    const code = typeof problem.code === 'string' ? problem.code : undefined;

    return new AppError({
        kind: kindOf(response.status),
        status: response.status,
        code,
        fieldCodes: readFieldCodes(problem.errorCodes),
        message: typeof problem.title === 'string' ? problem.title : `HTTP ${response.status}`
    });
}

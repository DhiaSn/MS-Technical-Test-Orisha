export interface Session {
    userId: string;
    username: string;
    displayName: string;
    role: string;
    accessTokenExpiresAt: string;
}

export interface Credentials {
    username: string;
    password: string;
}

export interface Registration extends Credentials {
    displayName: string;
}

export interface PasswordPolicy {
    minimumLength: number;
    requireUppercase: boolean;
    requireLowercase: boolean;
    requireDigit: boolean;
}

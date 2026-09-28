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

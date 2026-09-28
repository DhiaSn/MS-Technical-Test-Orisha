export const navigation = {
    replace: jest.fn(),
    pathname: '/reception',
    search: ''
};

export function mockLocation(pathname: string, search = ''): void {
    navigation.pathname = pathname;
    navigation.search = search;
}

export function resetNavigation(): void {
    navigation.replace.mockReset();
    mockLocation('/reception');
}

// Next hands out one router for the life of the app; a fresh object per render would re-run effects.
const router = { replace: navigation.replace };

export const nextNavigationMock = {
    useRouter: () => router,
    usePathname: () => navigation.pathname,
    useSearchParams: () => new URLSearchParams(navigation.search)
};

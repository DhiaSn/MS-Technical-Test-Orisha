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

export const nextNavigationMock = {
    useRouter: () => ({ replace: navigation.replace }),
    usePathname: () => navigation.pathname,
    useSearchParams: () => new URLSearchParams(navigation.search)
};

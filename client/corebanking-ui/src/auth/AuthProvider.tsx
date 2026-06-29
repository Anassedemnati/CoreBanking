import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import keycloak from './keycloak';
import { type Role, ROLE_LABELS } from './roles';

interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  email: string;
  roles: Role[];
  primaryRole: Role | null;
}

interface AuthContextValue {
  user: AuthUser | null;
  token: string | undefined;
  isAuthenticated: boolean;
  hasRole: (...roles: Role[]) => boolean;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function buildUser(): AuthUser | null {
  const tokenParsed = keycloak.tokenParsed as Record<string, unknown> | undefined;
  if (!keycloak.authenticated || !tokenParsed) return null;
  const realmRoles = (
    (tokenParsed['realm_access'] as { roles?: string[] } | undefined)?.roles ?? []
  ) as Role[];
  return {
    id: keycloak.subject ?? '',
    username: (tokenParsed['preferred_username'] as string) ?? '',
    fullName: (tokenParsed['name'] as string) ?? (tokenParsed['preferred_username'] as string) ?? '',
    email: (tokenParsed['email'] as string) ?? '',
    roles: realmRoles,
    primaryRole: realmRoles[0] ?? null,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Initialize synchronously from Keycloak so the first render is already authenticated.
  // (Keycloak.init() completed before React mounted, so keycloak.authenticated is already true.)
  const [user, setUser] = useState<AuthUser | null>(() => buildUser());
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => keycloak.authenticated ?? false);

  useEffect(() => {
    // Re-sync in case token was refreshed after mount
    setUser(buildUser());
    setIsAuthenticated(keycloak.authenticated ?? false);

    // Refresh token silently before expiry
    const refreshInterval = setInterval(() => {
      keycloak.updateToken(60)
        .then((refreshed) => { if (refreshed) { setUser(buildUser()); } })
        .catch(() => keycloak.logout());
    }, 30_000);

    return () => clearInterval(refreshInterval);
  }, []);

  const hasRole = (...roles: Role[]) =>
    roles.some((r) => user?.roles.includes(r));

  const logout = () =>
    keycloak.logout({ redirectUri: window.location.origin });

  return (
    <AuthContext.Provider value={{ user, token: keycloak.token, isAuthenticated, hasRole, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}

export { ROLE_LABELS };

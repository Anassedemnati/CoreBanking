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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const tokenParsed = keycloak.tokenParsed as Record<string, unknown> | undefined;
    if (keycloak.authenticated && tokenParsed) {
      const realmRoles = (
        (tokenParsed['realm_access'] as { roles?: string[] } | undefined)?.roles ?? []
      ) as Role[];

      const firstRole = realmRoles[0] ?? null;

      setUser({
        id: keycloak.subject ?? '',
        username: (tokenParsed['preferred_username'] as string) ?? '',
        fullName: (tokenParsed['name'] as string) ?? (tokenParsed['preferred_username'] as string) ?? '',
        email: (tokenParsed['email'] as string) ?? '',
        roles: realmRoles,
        primaryRole: firstRole,
      });
      setIsAuthenticated(true);
    }

    // Refresh token silently before expiry
    const refreshInterval = setInterval(() => {
      keycloak.updateToken(60).catch(() => keycloak.logout());
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

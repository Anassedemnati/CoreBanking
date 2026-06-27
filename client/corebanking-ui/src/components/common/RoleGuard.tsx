import type { ReactNode } from 'react';
import { useAuth } from '../../auth/AuthProvider';
import type { Role } from '../../auth/roles';

interface Props {
  roles: readonly Role[];
  children: ReactNode;
  fallback?: ReactNode;
}

export function RoleGuard({ roles, children, fallback = null }: Props) {
  const { hasRole } = useAuth();
  return hasRole(...(roles as Role[])) ? <>{children}</> : <>{fallback}</>;
}

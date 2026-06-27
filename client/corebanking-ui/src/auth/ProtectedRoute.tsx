import { Navigate } from 'react-router-dom';
import { useAuth } from './AuthProvider';
import { type Role } from './roles';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  roles?: readonly Role[];
}

export function ProtectedRoute({ children, roles }: Props) {
  const { isAuthenticated, hasRole } = useAuth();

  if (!isAuthenticated) return <Navigate to="/unauthorized" replace />;
  if (roles && roles.length > 0 && !hasRole(...(roles as Role[])))
    return <Navigate to="/unauthorized" replace />;

  return <>{children}</>;
}

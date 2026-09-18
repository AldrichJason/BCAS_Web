import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { RoleCode } from '@/api/types';
import { useAuth } from '@/features/auth/useAuth';
import { dashboardRouteForRole } from '@/features/auth/roles';

interface ProtectedRouteProps {
  children: ReactNode;
  /** When set, only these roles may see the route. */
  allow?: RoleCode[];
}

export function ProtectedRoute({ children, allow }: ProtectedRouteProps) {
  const { user, isRestoring } = useAuth();
  const location = useLocation();

  if (isRestoring) {
    return <p className="page-status">Loading&hellip;</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (allow && !allow.includes(user.roleCode)) {
    // Signed in but out of scope: send them to their own dashboard rather than
    // showing a dead end.
    return <Navigate to={dashboardRouteForRole(user.roleCode)} replace />;
  }

  return <>{children}</>;
}

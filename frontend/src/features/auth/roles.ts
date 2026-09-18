import type { RoleCode } from '@/api/types';

/** Landing route for each role after a successful sign-in. */
const DASHBOARD_ROUTES: Record<RoleCode, string> = {
  SUPER_ADMIN: '/admin',
  ACADEMIC_HEAD: '/department',
  REGISTRAR: '/registrar',
  VP_OPERATIONS: '/operations',
};

const FALLBACK_ROUTE = '/admin';

export function dashboardRouteForRole(role: RoleCode): string {
  return DASHBOARD_ROUTES[role] ?? FALLBACK_ROUTE;
}

export function isRoleCode(value: string): value is RoleCode {
  return value in DASHBOARD_ROUTES;
}

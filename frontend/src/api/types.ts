/** Mirrors the API's DepartmentScope. */
export interface DepartmentScope {
  departmentId: number;
  code: string;
  name: string;
}

/** Role codes issued by the API. Must match auth.Roles.Code in the database. */
export type RoleCode = 'SUPER_ADMIN' | 'ACADEMIC_HEAD' | 'REGISTRAR' | 'VP_OPERATIONS';

/** Mirrors the API's AuthenticatedUser. */
export interface AuthenticatedUser {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: RoleCode;
  roleName: string;
  mustChangePassword: boolean;
  departments: DepartmentScope[];
}

/** Mirrors the API's LoginResponse. */
export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

/** Mirrors the API's ApiError. */
export interface ApiErrorBody {
  code: string;
  message: string;
}

/** Mirrors the API's SessionResponse (BW-12). */
export interface SessionResponse {
  user: AuthenticatedUser;
}

/** Mirrors the API's ApiMessage. */
export interface ApiMessageBody {
  message: string;
}

/** Mirrors the API's UserAccount (BW-14, BW-15). */
export interface UserAccount {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: RoleCode;
  roleName: string;
  primaryDepartmentId: number | null;
  primaryDepartmentCode: string | null;
  primaryDepartmentName: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface RoleOption {
  code: RoleCode;
  name: string;
  description: string | null;
  /** True when a department must be chosen alongside this role. */
  requiresDepartment: boolean;
}

export interface DepartmentOption {
  departmentId: number;
  code: string;
  name: string;
}

/** Mirrors the API's AccountReference. */
export interface AccountReference {
  roles: RoleOption[];
  departments: DepartmentOption[];
}

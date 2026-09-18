import { request } from './client';
import type { AccountReference, UserAccount } from './types';

const BASE = '/api/admin/accounts';

/** BW-14/BW-15: every account, deactivated ones included. Super Admin only. */
export function listAccounts(token: string, signal?: AbortSignal): Promise<UserAccount[]> {
  return request<UserAccount[]>(BASE, { token, signal });
}

/** Role and department options for the create-account form. */
export function fetchAccountReference(
  token: string,
  signal?: AbortSignal,
): Promise<AccountReference> {
  return request<AccountReference>(`${BASE}/reference`, { token, signal });
}

export interface CreateAccountInput {
  firstName: string;
  lastName: string;
  email: string;
  roleCode: string;
  departmentId: number | null;
}

/** BW-14: provisions an account and triggers its invitation email. */
export function createAccount(
  token: string,
  input: CreateAccountInput,
  signal?: AbortSignal,
): Promise<UserAccount> {
  return request<UserAccount>(BASE, { method: 'POST', body: input, token, signal });
}

/** BW-15: flips the account between active and inactive. Never deletes. */
export function setAccountActivation(
  token: string,
  userId: number,
  isActive: boolean,
  signal?: AbortSignal,
): Promise<UserAccount> {
  return request<UserAccount>(`${BASE}/${userId}/activation`, {
    method: 'PUT',
    body: { isActive },
    token,
    signal,
  });
}

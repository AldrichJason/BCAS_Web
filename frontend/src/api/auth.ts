import { request } from './client';
import type { ApiMessageBody, LoginResponse, SessionResponse } from './types';

export function login(email: string, password: string, signal?: AbortSignal): Promise<LoginResponse> {
  return request<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: { email, password },
    signal,
  });
}

/** BW-12: validates the stored token and returns the current user. */
export function fetchSession(token: string, signal?: AbortSignal): Promise<SessionResponse> {
  return request<SessionResponse>('/api/auth/session', { token, signal });
}

/** BW-11: revokes the token server-side so it cannot be replayed. */
export function logout(token: string, signal?: AbortSignal): Promise<void> {
  return request<void>('/api/auth/logout', { method: 'POST', token, signal });
}

/** BW-13: always resolves the same way, whether or not the account exists. */
export function forgotPassword(email: string, signal?: AbortSignal): Promise<ApiMessageBody> {
  return request<ApiMessageBody>('/api/auth/forgot-password', {
    method: 'POST',
    body: { email },
    signal,
  });
}

/** BW-13: completes the reset using the token from the emailed link. */
export function resetPassword(
  token: string,
  newPassword: string,
  confirmPassword: string,
  signal?: AbortSignal,
): Promise<ApiMessageBody> {
  return request<ApiMessageBody>('/api/auth/reset-password', {
    method: 'POST',
    body: { token, newPassword, confirmPassword },
    signal,
  });
}

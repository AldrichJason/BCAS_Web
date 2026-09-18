import { request } from './client';
import type { LoginResponse } from './types';

export function login(email: string, password: string, signal?: AbortSignal): Promise<LoginResponse> {
  return request<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: { email, password },
    signal,
  });
}

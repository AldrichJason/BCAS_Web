import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { login as loginRequest } from '@/api/auth';
import type { AuthenticatedUser } from '@/api/types';
import { AuthContext, type AuthState } from './authContext';

const STORAGE_KEY = 'bcas.session';

interface StoredSession {
  token: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

/**
 * The session lives in sessionStorage, so it is scoped to the tab and cleared
 * when the browser closes. Server-side termination is BW-11.
 */
function readStoredSession(): StoredSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as StoredSession;
    if (!parsed.token || !parsed.user || Date.parse(parsed.expiresAt) <= Date.now()) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);

  useEffect(() => {
    const stored = readStoredSession();
    if (stored) {
      setUser(stored.user);
      setToken(stored.token);
    }
    setIsRestoring(false);
  }, []);

  const signIn = useCallback(async (email: string, password: string, signal?: AbortSignal) => {
    const response = await loginRequest(email, password, signal);
    setUser(response.user);
    setToken(response.accessToken);
    try {
      sessionStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          token: response.accessToken,
          expiresAt: response.expiresAt,
          user: response.user,
        } satisfies StoredSession),
      );
    } catch {
      // Storage can be unavailable (private mode, quota). The session still
      // works for this page load; it just will not survive a refresh.
    }
    return response.user;
  }, []);

  const signOut = useCallback(() => {
    setUser(null);
    setToken(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing to clean up if storage is unavailable.
    }
  }, []);

  const value = useMemo<AuthState>(
    () => ({ user, token, isRestoring, signIn, signOut }),
    [user, token, isRestoring, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

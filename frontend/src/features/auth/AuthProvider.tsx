import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { fetchSession, login as loginRequest, logout as logoutRequest } from '@/api/auth';
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
 * when the browser closes. It is only a cache: what the server says on the
 * session check wins.
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

function writeStoredSession(session: StoredSession): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Storage can be unavailable (private mode, quota). The session still works
    // for this page load; it just will not survive a refresh.
  }
}

function clearStoredSession(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing to clean up if storage is unavailable.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);

  // Kept in a ref so signOut can read the current token without being
  // recreated on every token change.
  const tokenRef = useRef<string | null>(null);
  tokenRef.current = token;

  // BW-12: confirm the stored token with the server on load. A token that is
  // expired, revoked by a logout, or belongs to a deactivated account is
  // rejected here, so the app never renders admin screens for a dead session.
  useEffect(() => {
    const stored = readStoredSession();

    if (!stored) {
      setIsRestoring(false);
      return;
    }

    const controller = new AbortController();
    let cancelled = false;

    void (async () => {
      try {
        const { user: current } = await fetchSession(stored.token, controller.signal);
        if (cancelled) {
          return;
        }
        setUser(current);
        setToken(stored.token);
        // Role or department scope may have changed since the token was issued.
        writeStoredSession({ ...stored, user: current });
      } catch {
        if (cancelled) {
          return;
        }
        clearStoredSession();
        setUser(null);
        setToken(null);
      } finally {
        if (!cancelled) {
          setIsRestoring(false);
        }
      }
    })();

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, []);

  const signIn = useCallback(async (email: string, password: string, signal?: AbortSignal) => {
    const response = await loginRequest(email, password, signal);
    setUser(response.user);
    setToken(response.accessToken);
    writeStoredSession({
      token: response.accessToken,
      expiresAt: response.expiresAt,
      user: response.user,
    });
    return response.user;
  }, []);

  // BW-11: revoke the token server-side, then drop every trace of it locally.
  const signOut = useCallback(async () => {
    const current = tokenRef.current;

    // Clear local state first, so the UI leaves the admin screens immediately
    // even if the revocation call is slow or fails.
    setUser(null);
    setToken(null);
    clearStoredSession();

    if (!current) {
      return;
    }

    try {
      await logoutRequest(current);
    } catch {
      // The token is already gone from this browser. A failed revocation is
      // logged server-side; there is nothing useful to show the user here.
    }
  }, []);

  const value = useMemo<AuthState>(
    () => ({ user, token, isRestoring, signIn, signOut }),
    [user, token, isRestoring, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

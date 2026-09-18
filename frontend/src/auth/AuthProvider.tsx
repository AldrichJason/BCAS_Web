import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { setAccessToken, setRefreshHandler } from '../api/client'
import { authApi } from '../api/endpoints'
import type { AuthResponse, CurrentUser } from '../api/types'
import { AuthContext, type AuthContextValue } from './AuthContext'

const REFRESH_TOKEN_KEY = 'bcas.refreshToken'

function readStoredRefreshToken(): string | null {
  try {
    return window.localStorage.getItem(REFRESH_TOKEN_KEY)
  } catch {
    return null
  }
}

function writeStoredRefreshToken(token: string | null): void {
  try {
    if (token) {
      window.localStorage.setItem(REFRESH_TOKEN_KEY, token)
    } else {
      window.localStorage.removeItem(REFRESH_TOKEN_KEY)
    }
  } catch {
    // Private browsing modes can refuse storage; the session then lasts until reload.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [initialising, setInitialising] = useState(true)

  const applySession = useCallback((auth: AuthResponse) => {
    setAccessToken(auth.accessToken)
    writeStoredRefreshToken(auth.refreshToken)
    setUser(auth.user)
  }, [])

  const clearSession = useCallback(() => {
    setAccessToken(null)
    writeStoredRefreshToken(null)
    setUser(null)
  }, [])

  const renewSession = useCallback(async (): Promise<string | null> => {
    const refreshToken = readStoredRefreshToken()

    if (!refreshToken) {
      return null
    }

    try {
      const auth = await authApi.refresh(refreshToken)
      applySession(auth)

      return auth.accessToken
    } catch {
      clearSession()

      return null
    }
  }, [applySession, clearSession])

  // Lets the API client renew an expired access token and replay the request once.
  useEffect(() => {
    setRefreshHandler(renewSession)

    return () => setRefreshHandler(null)
  }, [renewSession])

  // Restores the session left behind by a previous visit.
  useEffect(() => {
    let cancelled = false

    async function restore() {
      await renewSession()

      if (!cancelled) {
        setInitialising(false)
      }
    }

    void restore()

    return () => {
      cancelled = true
    }
  }, [renewSession])

  const login = useCallback(
    async (email: string, password: string) => {
      applySession(await authApi.login(email, password))
    },
    [applySession],
  )

  const logout = useCallback(async () => {
    const refreshToken = readStoredRefreshToken()

    if (refreshToken) {
      try {
        await authApi.logout(refreshToken)
      } catch {
        // The local session is dropped even when the server call fails.
      }
    }

    clearSession()
  }, [clearSession])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      initialising,
      canManageContent: user?.role === 'Administrator' || user?.role === 'Editor',
      isAdministrator: user?.role === 'Administrator',
      login,
      logout,
    }),
    [user, initialising, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

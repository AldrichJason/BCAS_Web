import { createContext } from 'react'
import type { CurrentUser } from '../api/types'

export interface AuthContextValue {
  user: CurrentUser | null
  /** False while the stored refresh token is being exchanged on first load. */
  initialising: boolean
  canManageContent: boolean
  isAdministrator: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

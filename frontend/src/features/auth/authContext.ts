import { createContext } from 'react';
import type { AuthenticatedUser } from '@/api/types';

export interface AuthState {
  user: AuthenticatedUser | null;
  token: string | null;
  /** True while the stored session is being restored on first render. */
  isRestoring: boolean;
  signIn: (email: string, password: string, signal?: AbortSignal) => Promise<AuthenticatedUser>;
  signOut: () => void;
}

export const AuthContext = createContext<AuthState | undefined>(undefined);

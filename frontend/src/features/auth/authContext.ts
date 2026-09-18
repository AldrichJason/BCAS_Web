import { createContext } from 'react';
import type { AuthenticatedUser } from '@/api/types';

export interface AuthState {
  user: AuthenticatedUser | null;
  token: string | null;
  /**
   * True until the session check that runs on app load has settled. Routes must
   * wait on this instead of assuming a missing user means "signed out" (BW-12).
   */
  isRestoring: boolean;
  signIn: (email: string, password: string, signal?: AbortSignal) => Promise<AuthenticatedUser>;
  signOut: () => Promise<void>;
}

export const AuthContext = createContext<AuthState | undefined>(undefined);

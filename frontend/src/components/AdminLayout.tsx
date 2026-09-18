import { useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/useAuth';
import './AdminLayout.css';

/**
 * Shell for every signed-in screen. It carries the sign-out control, so BW-11's
 * "available from every admin screen" holds by construction rather than by each
 * page remembering to add one.
 */
export function AdminLayout({ title, children }: { title: string; children?: ReactNode }) {
  const { user, signOut } = useAuth();
  const navigate = useNavigate();
  const [isSigningOut, setIsSigningOut] = useState(false);

  if (!user) {
    return null;
  }

  async function handleSignOut() {
    setIsSigningOut(true);
    try {
      await signOut();
      // replace: the admin screen is dropped from history, so Back cannot
      // return to it after signing out.
      navigate('/login', { replace: true });
    } finally {
      setIsSigningOut(false);
    }
  }

  return (
    <div className="admin-shell">
      <header className="admin-bar">
        <div className="admin-identity">
          <span className="admin-name">
            {user.firstName} {user.lastName}
          </span>
          <span className="admin-role">{user.roleName}</span>
        </div>
        <button
          type="button"
          className="admin-signout"
          onClick={() => void handleSignOut()}
          disabled={isSigningOut}
        >
          {isSigningOut ? 'Signing out…' : 'Sign out'}
        </button>
      </header>

      <main className="admin-main">
        <h1 className="admin-title">{title}</h1>
        {children}
      </main>
    </div>
  );
}

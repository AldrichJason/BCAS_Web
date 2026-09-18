import { useAuth } from '@/features/auth/useAuth';
import './DashboardPage.css';

/**
 * Placeholder dashboard shell. Each role lands here after sign-in; the modules
 * themselves arrive with the content-management stories.
 */
export function DashboardPage({ title }: { title: string }) {
  const { user, signOut } = useAuth();

  if (!user) {
    return null;
  }

  const scope =
    user.departments.length > 0
      ? user.departments.map((d) => d.name).join(', ')
      : 'School-wide';

  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <div>
          <h1>{title}</h1>
          <p className="dashboard-subtitle">
            {user.firstName} {user.lastName} &middot; {user.roleName}
          </p>
        </div>
        <button type="button" className="dashboard-signout" onClick={signOut}>
          Sign out
        </button>
      </header>

      {user.mustChangePassword && (
        <p className="dashboard-notice" role="status">
          Your account is still using its initial password. Change it before you continue.
        </p>
      )}

      <section className="dashboard-panel">
        <h2>Your scope</h2>
        <p>{scope}</p>
      </section>
    </main>
  );
}

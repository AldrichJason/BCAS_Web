import { Link } from 'react-router-dom';
import { AdminLayout } from '@/components/AdminLayout';
import { useAuth } from '@/features/auth/useAuth';
import './DashboardPage.css';

/**
 * Placeholder dashboard shell. Each role lands here after sign-in; the modules
 * themselves arrive with the content-management stories.
 */
export function DashboardPage({ title }: { title: string }) {
  const { user } = useAuth();

  if (!user) {
    return null;
  }

  const scope =
    user.departments.length > 0 ? user.departments.map((d) => d.name).join(', ') : 'School-wide';

  return (
    <AdminLayout title={title}>
      {user.mustChangePassword && (
        <p className="dashboard-notice" role="status">
          Your account is still using its initial password. Change it before you continue.
        </p>
      )}

      <section className="dashboard-panel">
        <h2>Your scope</h2>
        <p>{scope}</p>
      </section>

      {user.roleCode === 'SUPER_ADMIN' && (
        <section className="dashboard-panel">
          <h2>Administration</h2>
          <p>Create accounts, assign roles and department scope, and activate or deactivate staff.</p>
          <Link className="dashboard-link" to="/admin/accounts">
            Manage accounts
          </Link>
        </section>
      )}
    </AdminLayout>
  );
}

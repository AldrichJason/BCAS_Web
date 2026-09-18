import { useCallback, useEffect, useState } from 'react';
import { fetchAccountReference, listAccounts, setAccountActivation } from '@/api/accounts';
import { ApiError } from '@/api/client';
import type { AccountReference, UserAccount } from '@/api/types';
import { AdminLayout } from '@/components/AdminLayout';
import { useAuth } from '@/features/auth/useAuth';
import { CreateAccountForm } from './CreateAccountForm';
import './AccountsPage.css';

function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleDateString() : '—';
}

export function AccountsPage() {
  const { token, user } = useAuth();

  const [accounts, setAccounts] = useState<UserAccount[]>([]);
  const [reference, setReference] = useState<AccountReference | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  /** User id whose activation toggle is mid-flight. */
  const [pendingUserId, setPendingUserId] = useState<number | null>(null);

  useEffect(() => {
    if (!token) {
      return;
    }

    const controller = new AbortController();
    let cancelled = false;

    void (async () => {
      try {
        const [loadedAccounts, loadedReference] = await Promise.all([
          listAccounts(token, controller.signal),
          fetchAccountReference(token, controller.signal),
        ]);
        if (cancelled) {
          return;
        }
        setAccounts(loadedAccounts);
        setReference(loadedReference);
      } catch (error) {
        if (cancelled || (error instanceof DOMException && error.name === 'AbortError')) {
          return;
        }
        setLoadError(
          error instanceof ApiError ? error.message : 'Could not load accounts. Please try again.',
        );
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [token]);

  const upsert = useCallback((updated: UserAccount) => {
    setAccounts((current) => {
      const index = current.findIndex((a) => a.userId === updated.userId);
      if (index === -1) {
        return [updated, ...current];
      }
      const next = [...current];
      next[index] = updated;
      return next;
    });
  }, []);

  async function handleToggle(account: UserAccount) {
    if (!token) {
      return;
    }

    setActionError(null);
    setNotice(null);
    setPendingUserId(account.userId);

    try {
      const updated = await setAccountActivation(token, account.userId, !account.isActive);
      upsert(updated);
      setNotice(
        updated.isActive
          ? `${updated.firstName} ${updated.lastName} can sign in again.`
          : `${updated.firstName} ${updated.lastName} has been deactivated. Their content and history are kept.`,
      );
    } catch (error) {
      setActionError(
        error instanceof ApiError ? error.message : 'Could not update that account. Please try again.',
      );
    } finally {
      setPendingUserId(null);
    }
  }

  return (
    <AdminLayout title="Accounts">
      {isLoading && <p className="page-status">Loading accounts&hellip;</p>}

      {loadError && (
        <p className="auth-error" role="alert">
          {loadError}
        </p>
      )}

      {!isLoading && !loadError && (
        <>
          {notice && (
            <p className="auth-success" role="status">
              {notice}
            </p>
          )}
          {actionError && (
            <p className="auth-error" role="alert">
              {actionError}
            </p>
          )}

          <div className="account-table-wrap">
            <table className="account-table">
              <caption className="visually-hidden">
                All portal accounts, including deactivated ones
              </caption>
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Email</th>
                  <th scope="col">Role</th>
                  <th scope="col">Department</th>
                  <th scope="col">Last sign-in</th>
                  <th scope="col">Status</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {accounts.map((account) => {
                  const isSelf = account.userId === user?.userId;
                  return (
                    <tr key={account.userId} className={account.isActive ? undefined : 'is-inactive'}>
                      <td>
                        {account.firstName} {account.lastName}
                        {isSelf && <span className="account-you"> (you)</span>}
                      </td>
                      <td>{account.email}</td>
                      <td>{account.roleName}</td>
                      <td>{account.primaryDepartmentName ?? 'School-wide'}</td>
                      <td>{formatDate(account.lastLoginAt)}</td>
                      <td>
                        <span className={account.isActive ? 'badge badge-active' : 'badge badge-inactive'}>
                          {account.isActive ? 'Active' : 'Inactive'}
                        </span>
                        {account.mustChangePassword && account.isActive && (
                          <span className="badge badge-pending">Invitation pending</span>
                        )}
                      </td>
                      <td className="account-actions">
                        <button
                          type="button"
                          className="account-toggle"
                          disabled={isSelf || pendingUserId === account.userId}
                          title={isSelf ? 'You cannot deactivate your own account.' : undefined}
                          onClick={() => void handleToggle(account)}
                        >
                          {pendingUserId === account.userId
                            ? 'Saving…'
                            : account.isActive
                              ? 'Deactivate'
                              : 'Activate'}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {reference && token && (
            <CreateAccountForm
              token={token}
              reference={reference}
              onCreated={(created) => {
                upsert(created);
                setNotice(`Invitation sent to ${created.email}.`);
                setActionError(null);
              }}
            />
          )}
        </>
      )}
    </AdminLayout>
  );
}

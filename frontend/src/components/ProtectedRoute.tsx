import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Loading } from './StatusMessage'

export function ProtectedRoute({ children, adminOnly = false }: { children: ReactNode; adminOnly?: boolean }) {
  const { user, initialising, canManageContent, isAdministrator } = useAuth()
  const location = useLocation()

  if (initialising) {
    return <Loading label="Checking your session…" />
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  const permitted = adminOnly ? isAdministrator : canManageContent

  if (!permitted) {
    return (
      <section className="panel">
        <h1>Not allowed</h1>
        <p>Your account does not have access to this part of the CMS.</p>
      </section>
    )
  }

  return <>{children}</>
}

import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ChatWidget } from './ChatWidget'

const publicLinks = [
  { to: '/', label: 'Home', end: true },
  { to: '/announcements', label: 'News' },
  { to: '/programs', label: 'Programs' },
  { to: '/pages/about', label: 'About' },
  { to: '/pages/admission', label: 'Admission' },
  { to: '/pages/contact', label: 'Contact' },
]

export function Layout() {
  const { user, canManageContent, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className="app">
      <header className="masthead">
        <div className="masthead__inner">
          <NavLink to="/" className="brand">
            <span className="brand__mark">BCAS</span>
            <span className="brand__text">Batangas College of Arts and Sciences</span>
          </NavLink>

          <nav className="nav" aria-label="Main">
            {publicLinks.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) => (isActive ? 'nav__link nav__link--active' : 'nav__link')}
              >
                {link.label}
              </NavLink>
            ))}
          </nav>

          <div className="masthead__account">
            {canManageContent && (
              <NavLink to="/admin" className="nav__link">
                CMS
              </NavLink>
            )}
            {user ? (
              <button type="button" className="button button--ghost" onClick={() => void handleLogout()}>
                Sign out
              </button>
            ) : (
              <NavLink to="/login" className="button button--ghost">
                Sign in
              </NavLink>
            )}
          </div>
        </div>
      </header>

      <main className="main">
        <Outlet />
      </main>

      <footer className="footer">
        <p>© {new Date().getFullYear()} Batangas College of Arts and Sciences. All rights reserved.</p>
        <p>registrar@bcas.edu.ph · (043) 000-0000</p>
      </footer>

      <ChatWidget />
    </div>
  )
}

import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'
import { Button } from './Button'

export function Layout() {
  const { authenticated, user, logout } = useAuth()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const flush =
    location.pathname === '/' ||
    location.pathname === '/login' ||
    location.pathname === '/register'

  useEffect(() => {
    setMenuOpen(false)
  }, [location.pathname])

  async function handleLogout() {
    await logout()
  }

  return (
    <div className="layout">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="header">
        <div className="container header-inner">
          <NavLink to="/" className="brand">
            <span className="brand-mark">K</span>
            Khidma
          </NavLink>
          <button
            type="button"
            className="nav-toggle"
            aria-expanded={menuOpen}
            aria-controls="site-nav"
            onClick={() => setMenuOpen((open) => !open)}
          >
            <span className="sr-only">Menu</span>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M4 7h16M4 12h16M4 17h16"
                stroke="currentColor"
                strokeWidth="1.8"
                strokeLinecap="round"
              />
            </svg>
          </button>
          <nav id="site-nav" className={menuOpen ? 'nav open' : 'nav'}>
            <NavLink to="/" className="nav-link" end>
              Home
            </NavLink>
            <NavLink to="/catalog" className="nav-link">
              Services
            </NavLink>
            <div className="nav-actions">
              {authenticated && user ? (
                <>
                  <div className="user-chip">
                    <strong>{user.fullName}</strong>
                    <span>{user.role}</span>
                  </div>
                  <NavLink to={dashboardPath(user.role)} className="nav-link">
                    Dashboard
                  </NavLink>
                  <Button variant="ghost" onClick={() => void handleLogout()}>
                    Logout
                  </Button>
                </>
              ) : (
                <>
                  <NavLink to="/login" className="nav-link">
                    Login
                  </NavLink>
                  <Button to="/register">Register</Button>
                </>
              )}
            </div>
          </nav>
        </div>
      </header>
      <main id="main" className={flush ? 'main main-flush' : 'main'}>
        {flush ? <Outlet /> : <div className="container"><Outlet /></div>}
      </main>
      <footer className="footer">
        <div className="container">
          <div className="footer-grid">
            <div>
              <h2>Khidma</h2>
              <p>
                A trusted marketplace for requesting local services, comparing
                offers, and booking with confidence.
              </p>
            </div>
            <div>
              <h3>Explore</h3>
              <ul>
                <li>
                  <NavLink to="/">Home</NavLink>
                </li>
                <li>
                  <NavLink to="/catalog">Services</NavLink>
                </li>
                <li>
                  {authenticated && user ? (
                    <NavLink to={dashboardPath(user.role)}>Dashboard</NavLink>
                  ) : (
                    <NavLink to="/register">Get started</NavLink>
                  )}
                </li>
              </ul>
            </div>
            <div>
              <h3>Account</h3>
              <ul>
                {authenticated && user ? (
                  <>
                    <li>
                      <NavLink to={dashboardPath(user.role)}>Dashboard</NavLink>
                    </li>
                    <li>
                      <button
                        type="button"
                        className="footer-action"
                        onClick={() => void handleLogout()}
                      >
                        Logout
                      </button>
                    </li>
                  </>
                ) : (
                  <>
                    <li>
                      <NavLink to="/login">Login</NavLink>
                    </li>
                    <li>
                      <NavLink to="/register">Register</NavLink>
                    </li>
                  </>
                )}
              </ul>
            </div>
          </div>
          <p className="footer-copy">© {new Date().getFullYear()} Khidma. All rights reserved.</p>
        </div>
      </footer>
    </div>
  )
}

import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { dashboardPath, Roles } from '../auth/roles'
import { BrandLink } from './BrandLink'

export function Footer() {
  const { authenticated, user, logout } = useAuth()

  async function handleLogout() {
    await logout()
  }

  const dashboardLabel =
    user?.role === Roles.Admin ? 'Admin Dashboard' : 'Dashboard'

  return (
    <footer className="footer">
      <div className="container">
        <div className="footer-grid">
          <div className="footer-brand">
            <BrandLink />
            <p>People. Services. A better way to get things done locally.</p>
          </div>
          <div>
            <h3>Quick Links</h3>
            <ul>
              <li>
                <NavLink to="/">Home</NavLink>
              </li>
              <li>
                <NavLink to="/catalog">Catalog</NavLink>
              </li>
              <li>
                <NavLink to="/#how-it-works">How It Works</NavLink>
              </li>
            </ul>
          </div>
          <div>
            <h3>Account</h3>
            <ul>
              {authenticated && user ? (
                <>
                  <li>
                    <NavLink to={dashboardPath(user.role)}>{dashboardLabel}</NavLink>
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
        <p className="footer-copy">
          © {new Date().getFullYear()} Khidma. All rights reserved.
        </p>
      </div>
    </footer>
  )
}

import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { dashboardPath, Roles } from '../auth/roles'

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

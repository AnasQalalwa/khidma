import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'

export function Layout() {
  const { authenticated, user, logout } = useAuth()

  async function handleLogout() {
    await logout()
  }

  return (
    <div className="layout">
      <header className="header">
        <div className="header-inner">
          <NavLink to="/" className="brand">
            Khidma
          </NavLink>
          <nav className="nav">
            <NavLink to="/">Home</NavLink>
            <NavLink to="/catalog">Catalog</NavLink>
            {authenticated && user ? (
              <>
                <NavLink to={dashboardPath(user.role)}>Dashboard</NavLink>
                <button type="button" className="btn linkish" onClick={() => void handleLogout()}>
                  Logout
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login">Login</NavLink>
                <NavLink to="/register">Register</NavLink>
              </>
            )}
          </nav>
        </div>
      </header>
      <main className="main">
        <div className="container">
          <Outlet />
        </div>
      </main>
      <footer className="footer">
        <div className="container">Khidma service booking platform</div>
      </footer>
    </div>
  )
}

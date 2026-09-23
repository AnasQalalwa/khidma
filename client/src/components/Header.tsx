import { useEffect, useState, type MouseEvent } from 'react'
import { ArrowRight, LogOut, Menu, X } from 'lucide-react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { dashboardPath, Roles, workspaceLinks } from '../auth/roles'
import { BrandLink } from './BrandLink'
import { RoleBadge } from './Badge'
import { Button, IconButton } from './Button'

export function Header() {
  const { authenticated, user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)
  const [accountOpen, setAccountOpen] = useState(false)

  function closeMenu() {
    setMenuOpen(false)
  }

  useEffect(() => {
    if (!menuOpen) {
      return
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setMenuOpen(false)
      }
    }

    document.addEventListener('keydown', onKeyDown)
    document.body.classList.add('nav-locked')

    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.classList.remove('nav-locked')
    }
  }, [menuOpen])

  useEffect(() => {
    function onResize() {
      if (window.innerWidth > 900) {
        setMenuOpen(false)
      }
    }

    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])

  function handleNavClick(event: MouseEvent<HTMLElement>) {
    const target = event.target
    if (!(target instanceof Element)) {
      return
    }

    if (target.closest('a, button')) {
      closeMenu()
    }
  }

  async function handleLogout() {
    closeMenu()
    await logout()
  }

  const dashboardLabel =
    user?.role === Roles.Admin ? 'Admin Dashboard' : 'Dashboard'
  const isCustomer = user?.role === Roles.Customer

  return (
    <header className="header">
      <div className="container header-inner">
        <BrandLink onClick={closeMenu} />
        <IconButton
          className="nav-toggle"
          label={menuOpen ? 'Close menu' : 'Open menu'}
          icon={menuOpen ? X : Menu}
          aria-expanded={menuOpen}
          aria-controls="site-nav"
          onClick={() => setMenuOpen((open) => !open)}
        />
        <nav
          id="site-nav"
          className={menuOpen ? 'nav open' : 'nav'}
          onClick={handleNavClick}
        >
          <NavLink to="/" className="nav-link" end>
            Home
          </NavLink>
          <NavLink to="/catalog" className="nav-link">
            Catalog
          </NavLink>
          <Link to="/#how-it-works" className="nav-link nav-desktop-only">
            How It Works
          </Link>
          <div className="nav-actions">
            {authenticated && user ? (
              <>
                <div className="user-menu">
                  <button
                    type="button"
                    className="user-chip"
                    aria-expanded={accountOpen}
                    aria-haspopup="menu"
                    onClick={() => setAccountOpen((open) => !open)}
                  >
                    <div className="user-chip-copy">
                      <strong>{user.fullName}</strong>
                    </div>
                    <RoleBadge role={user.role} />
                  </button>
                  {accountOpen ? (
                    <div className="user-menu-panel" role="menu">
                      <button
                        type="button"
                        role="menuitem"
                        onClick={() => void handleLogout()}
                      >
                        Logout
                      </button>
                    </div>
                  ) : null}
                </div>
                {isCustomer ? (
                  <>
                    <NavLink to="/account/bookings" className="nav-link">
                      My Bookings
                    </NavLink>
                    <NavLink to="/account" className="nav-link">
                      Profile
                    </NavLink>
                  </>
                ) : (
                  <NavLink to={dashboardPath(user.role)} className="nav-link">
                    {dashboardLabel}
                  </NavLink>
                )}
                {isCustomer
                  ? null
                  : workspaceLinks(user.role)
                  .filter((link) => link.to !== dashboardPath(user.role))
                  .map((link) => (
                    <NavLink
                      key={link.to}
                      to={link.to}
                      className="nav-link nav-mobile-only"
                    >
                      {link.label}
                    </NavLink>
                  ))}
                <Button
                  className="nav-mobile-only"
                  variant="ghost"
                  icon={LogOut}
                  onClick={() => void handleLogout()}
                >
                  Logout
                </Button>
              </>
            ) : (
              <>
                <NavLink to="/login" className="nav-link">
                  Login
                </NavLink>
                <NavLink to="/register" className="nav-link">
                  Register
                </NavLink>
                <Button to="/catalog" iconRight={ArrowRight}>
                  Find a Service
                </Button>
              </>
            )}
          </div>
        </nav>
      </div>
    </header>
  )
}

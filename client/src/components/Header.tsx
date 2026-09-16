import { useEffect, useState, type MouseEvent } from 'react'
import { ArrowRight, Home, LogOut, Menu, X } from 'lucide-react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { dashboardPath, Roles, workspaceLinks } from '../auth/roles'
import { RoleBadge } from './Badge'
import { Button, IconButton } from './Button'
import { Icon } from './icons'

export function Header() {
  const { authenticated, user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

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

  return (
    <header className="header">
      <div className="container header-inner">
        <NavLink to="/" className="brand" onClick={closeMenu}>
          <span className="brand-mark">
            <Icon icon={Home} size={16} />
          </span>
          Khidma
        </NavLink>
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
                <div className="user-chip">
                  <div className="user-chip-copy">
                    <strong>{user.fullName}</strong>
                  </div>
                  <RoleBadge role={user.role} />
                </div>
                <NavLink to={dashboardPath(user.role)} className="nav-link">
                  {dashboardLabel}
                </NavLink>
                {workspaceLinks(user.role)
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

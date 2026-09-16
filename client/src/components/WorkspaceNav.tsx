import { NavLink } from 'react-router-dom'
import { workspaceLinks } from '../auth/roles'

export function WorkspaceNav({ role }: { role: string }) {
  const links = workspaceLinks(role)

  return (
    <nav className="workspace-nav" aria-label="Workspace">
      {links.map((link) => (
        <NavLink
          key={link.to}
          to={link.to}
          end={'end' in link ? link.end : false}
          className={({ isActive }) =>
            isActive ? 'workspace-link is-active' : 'workspace-link'
          }
        >
          {link.label}
        </NavLink>
      ))}
    </nav>
  )
}

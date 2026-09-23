import {
  CalendarDays,
  ClipboardList,
  LayoutDashboard,
  ScrollText,
  ShieldCheck,
  Store,
  UserRound,
  Users,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { workspaceLinks } from '../auth/roles'
import { Icon } from './icons'

const ICONS = {
  Overview: LayoutDashboard,
  Schedule: CalendarDays,
  Bookings: ClipboardList,
  'My Bookings': ClipboardList,
  Profile: UserRound,
  Users: Users,
  'Provider Verification': ShieldCheck,
  Providers: Store,
  'Audit Logs': ScrollText,
  Catalog: Store,
} as const

export function WorkspaceNav({ role }: { role: string }) {
  const links = workspaceLinks(role)

  return (
    <nav className="workspace-nav" aria-label="Workspace">
      {links.map((link) => {
        const icon = ICONS[link.label as keyof typeof ICONS] ?? LayoutDashboard
        return (
          <NavLink
            key={link.to}
            to={link.to}
            end={'end' in link ? link.end : false}
            className={({ isActive }) =>
              isActive ? 'workspace-link is-active' : 'workspace-link'
            }
          >
            <Icon icon={icon} size={16} />
            {link.label}
          </NavLink>
        )
      })}
    </nav>
  )
}

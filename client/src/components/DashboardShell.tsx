import type { ReactNode } from 'react'
import { Badge } from './Badge'

export function DashboardShell({
  title,
  description,
  badge,
  variant = 'default',
  children,
}: {
  title: string
  description: string
  badge: ReactNode
  variant?: 'default' | 'admin'
  children: ReactNode
}) {
  return (
    <section className="dashboard-shell">
      <header
        className={
          variant === 'admin' ? 'dashboard-hero dashboard-hero-admin' : 'dashboard-hero'
        }
      >
        <div>
          {badge}
          <h1>{title}</h1>
          <p className="muted">{description}</p>
        </div>
      </header>
      {children}
    </section>
  )
}

export function DashboardPanel({
  title,
  description,
  comingSoon = false,
  children,
}: {
  title: string
  description?: string
  comingSoon?: boolean
  children?: ReactNode
}) {
  return (
    <article className="dashboard-panel">
      <div className="dashboard-panel-head">
        <h2>{title}</h2>
        {comingSoon ? <Badge tone="muted">Coming soon</Badge> : null}
      </div>
      {description ? <p className="muted">{description}</p> : null}
      {children}
    </article>
  )
}

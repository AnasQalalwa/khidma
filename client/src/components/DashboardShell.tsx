import type { ReactNode } from 'react'

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
  children,
}: {
  title: string
  description?: string
  children?: ReactNode
}) {
  return (
    <article className="dashboard-panel">
      <div className="dashboard-panel-head">
        <h2>{title}</h2>
      </div>
      {description ? <p className="muted">{description}</p> : null}
      {children}
    </article>
  )
}

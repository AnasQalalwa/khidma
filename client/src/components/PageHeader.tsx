import type { ReactNode } from 'react'

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow?: string
  title: string
  description?: string
  actions?: ReactNode
}) {
  return (
    <header className="page-header">
      {eyebrow ? <span className="eyebrow">{eyebrow}</span> : null}
      <div className="page-header-row">
        <div>
          <h1>{title}</h1>
          {description ? <p className="lead">{description}</p> : null}
        </div>
        {actions}
      </div>
    </header>
  )
}

export function SectionHeader({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow?: string
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="section-header">
      {eyebrow ? <span className="eyebrow">{eyebrow}</span> : null}
      <div className="section-header-row">
        <div>
          <h2>{title}</h2>
          {description ? <p className="muted">{description}</p> : null}
        </div>
        {action}
      </div>
    </div>
  )
}

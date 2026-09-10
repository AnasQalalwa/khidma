import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

type CategoryIconKind = 'home' | 'technology' | 'cleaning' | 'tutoring' | 'service'

function categoryIconKind(name: string): CategoryIconKind {
  const key = name.toLowerCase()

  if (/(clean|maid|laundry)/.test(key)) {
    return 'cleaning'
  }

  if (/(tutor|educat|learn|school)/.test(key)) {
    return 'tutoring'
  }

  if (/(tech|computer|phone|network)/.test(key)) {
    return 'technology'
  }

  if (/(home|plumb|electric|paint|carpent|handyman)/.test(key)) {
    return 'home'
  }

  return 'service'
}

function CategoryIcon({ name }: { name: string }) {
  const kind = categoryIconKind(name)

  return (
    <div className={`icon-wrap icon-${kind}`} aria-hidden="true">
      {kind === 'home' ? (
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path
            d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5Z"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinejoin="round"
          />
        </svg>
      ) : null}
      {kind === 'technology' ? (
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <rect x="3.5" y="5" width="17" height="12" rx="2" stroke="currentColor" strokeWidth="1.8" />
          <path d="M8 20h8M12 17v3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
        </svg>
      ) : null}
      {kind === 'cleaning' ? (
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path
            d="M5 14c2.2-1.8 4.4-3 7-3s4.8 1.2 7 3"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
          />
          <path
            d="M8 9.5 9.2 7M12 8.5V6M16 9.5 14.8 7"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
          />
          <path d="M7 17h10" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
        </svg>
      ) : null}
      {kind === 'tutoring' ? (
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path
            d="M4 7.5 12 4l8 3.5v9.5L12 20l-8-3.5V7.5Z"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinejoin="round"
          />
          <path d="M12 8v12" stroke="currentColor" strokeWidth="1.8" />
        </svg>
      ) : null}
      {kind === 'service' ? (
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path
            d="M14.5 6.5 17 4l3 3-2.5 2.5M14.5 6.5 7 14v3h3l7.5-7.5Z"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinejoin="round"
          />
        </svg>
      ) : null}
    </div>
  )
}

export function CategoryCard({
  name,
  serviceCount,
  to,
}: {
  name: string
  serviceCount: number
  to?: string
}) {
  const content = (
    <>
      <CategoryIcon name={name} />
      <h3>{name}</h3>
      <p className="count">
        {serviceCount} {serviceCount === 1 ? 'service' : 'services'}
      </p>
    </>
  )

  if (to) {
    return (
      <Link className="category-card" to={to}>
        {content}
      </Link>
    )
  }

  return <article className="category-card">{content}</article>
}

export function ServiceCard({
  name,
  categoryName,
}: {
  name: string
  categoryName: string
}) {
  return (
    <article className="service-card">
      <p className="meta">{categoryName}</p>
      <h3>{name}</h3>
      <p className="coming-soon">Available for future booking requests</p>
    </article>
  )
}

export function DashboardStatCard({
  title,
  hint,
}: {
  title: string
  hint: string
}) {
  return (
    <article className="stat-card">
      <p className="meta">{title}</p>
      <p className="value">--</p>
      <p className="coming-soon">{hint}</p>
    </article>
  )
}

export function EmptyState({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <div className="empty-state status-block">
      <h2>{title}</h2>
      <p className="muted">{description}</p>
    </div>
  )
}

export function LoadingState({ label }: { label: string }) {
  return (
    <div aria-busy="true" aria-live="polite">
      <p className="sr-only">{label}</p>
      <div className="service-grid">
        <div className="skeleton" />
        <div className="skeleton" />
        <div className="skeleton" />
        <div className="skeleton" />
      </div>
    </div>
  )
}

export function AuthCard({
  title,
  description,
  children,
}: {
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <section className="auth-card">
      <h1>{title}</h1>
      <p className="muted">{description}</p>
      {children}
    </section>
  )
}

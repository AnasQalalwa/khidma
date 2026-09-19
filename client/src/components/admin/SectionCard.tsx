import type { ReactNode } from 'react'
import { ErrorState, Skeleton } from '../States'

export function SectionCard({
  title,
  status,
  error,
  onRetry,
  empty,
  children,
  className = '',
  action,
}: {
  title: string
  status: 'loading' | 'empty' | 'error' | 'ready'
  error?: string | null
  onRetry?: () => void
  empty?: string
  children?: ReactNode
  className?: string
  action?: ReactNode
}) {
  return (
    <section className={`admin-card ${className}`.trim()}>
      <div className="dashboard-panel-head">
        <h2 className="admin-section-title">{title}</h2>
        {action}
      </div>
      {status === 'loading' ? <Skeleton className="admin-skeleton" /> : null}
      {status === 'error' ? (
        <ErrorState
          title={`Unable to load ${title.toLowerCase()}`}
          description={error ?? 'Please try again.'}
          onRetry={onRetry}
        />
      ) : null}
      {status === 'empty' ? <p className="admin-empty">{empty ?? 'Nothing to show yet.'}</p> : null}
      {status === 'ready' ? children : null}
    </section>
  )
}

import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { AlertTriangle, Inbox, RotateCcw, SearchX } from 'lucide-react'
import { Button } from './Button'
import { IconTile } from './icons'

export function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`skeleton ${className}`.trim()} />
}

export function LoadingState({
  label,
  count = 4,
}: {
  label: string
  count?: number
}) {
  return (
    <div aria-busy="true" aria-live="polite">
      <p className="sr-only">{label}</p>
      <div className="service-grid">
        {Array.from({ length: count }, (_, index) => (
          <Skeleton key={index} />
        ))}
      </div>
    </div>
  )
}

export function EmptyState({
  title,
  description,
  icon = Inbox,
  action,
}: {
  title: string
  description: string
  icon?: LucideIcon
  action?: ReactNode
}) {
  return (
    <div className="empty-state status-block">
      <IconTile icon={icon} accent="service" />
      <h2>{title}</h2>
      <p className="muted">{description}</p>
      {action ? <div className="status-actions">{action}</div> : null}
    </div>
  )
}

export function ErrorState({
  title,
  description,
  onRetry,
}: {
  title: string
  description: string
  onRetry?: () => void
}) {
  return (
    <div className="error-state status-block" role="alert">
      <IconTile icon={AlertTriangle} accent="electrical" />
      <h2>{title}</h2>
      <p className="muted">{description}</p>
      {onRetry ? (
        <div className="status-actions">
          <Button variant="secondary" onClick={onRetry} icon={RotateCcw}>
            Try again
          </Button>
        </div>
      ) : null}
    </div>
  )
}

export function SearchEmptyState() {
  return (
    <EmptyState
      title="No matching services"
      description="Try another category or search term."
      icon={SearchX}
    />
  )
}

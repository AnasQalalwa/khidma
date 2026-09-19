import { AlertTriangle, CheckCircle2, Clock3, FileWarning, PauseCircle } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { AdminOverview } from '../../api/types'
import { formatCount } from '../../utils/format'

const ITEMS: {
  key: keyof AdminOverview['attention']
  label: string
  href: string
  icon: typeof Clock3
}[] = [
  {
    key: 'pendingVerifications',
    label: 'Pending verifications',
    href: '/admin/verifications?verificationStatus=PendingReview',
    icon: FileWarning,
  },
  {
    key: 'staleOpenRequests',
    label: 'Stale open requests',
    href: '/admin/catalog',
    icon: Clock3,
  },
  {
    key: 'overdueBookings',
    label: 'Overdue bookings',
    href: '/admin/audit',
    icon: AlertTriangle,
  },
  {
    key: 'suspendedProviders',
    label: 'Suspended providers',
    href: '/admin/providers?suspended=true',
    icon: PauseCircle,
  },
]

export function AttentionList({ attention }: { attention: AdminOverview['attention'] }) {
  const clear = ITEMS.every((item) => attention[item.key] === 0)
  if (clear) {
    return (
      <p className="admin-success-line">
        <CheckCircle2 size={16} aria-hidden="true" />
        Nothing needs attention
      </p>
    )
  }

  return (
    <div>
      {ITEMS.map((item) => {
        const count = attention[item.key]
        if (count === 0) {
          return null
        }

        const Icon = item.icon
        return (
          <Link key={item.key} className="attention-row" to={item.href}>
            <Icon size={18} aria-hidden="true" />
            <span>{item.label}</span>
            <span className="attention-count">{formatCount(count)}</span>
          </Link>
        )
      })}
    </div>
  )
}

import { ShieldAlert } from 'lucide-react'
import type { AdminOverview } from '../../api/types'
import { formatCount } from '../../utils/format'

export function SecurityStrip({ security }: { security: AdminOverview['security24h'] }) {
  return (
    <aside className="admin-card security-strip" aria-label="Security events in the last 24 hours">
      <ShieldAlert size={18} aria-hidden="true" />
      <span className="security-item">
        Failed logins <strong>{formatCount(security.failedLogins)}</strong>
      </span>
      <span className="security-item">
        Denied actions <strong>{formatCount(security.deniedActions)}</strong>
      </span>
      <span className="security-item">
        CSRF rejections <strong>{formatCount(security.csrfRejections)}</strong>
      </span>
    </aside>
  )
}

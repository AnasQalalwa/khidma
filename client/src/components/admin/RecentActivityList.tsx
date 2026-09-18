import type { AuditLogItem } from '../../api/types'
import { StatusBadge } from '../StatusBadge'
import { formatDate } from '../../utils/format'

export function RecentActivityList({ events }: { events: AuditLogItem[] }) {
  return (
    <ul className="admin-list">
      {events.map((event) => (
        <li key={event.id} className="admin-list-row">
          <div>
            <strong>{event.summary}</strong>
            <p className="muted">
              {formatDate(event.createdAt)} · {event.actorEmail ?? 'System'}
            </p>
          </div>
          <StatusBadge status={event.outcome} />
        </li>
      ))}
    </ul>
  )
}

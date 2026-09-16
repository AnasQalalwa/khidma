import { statusLabel } from '../utils/format'

const TONES: Record<string, string> = {
  Open: 'open',
  Booked: 'booked',
  Completed: 'success',
  Cancelled: 'danger',
  Pending: 'pending',
  Accepted: 'success',
  Rejected: 'danger',
  Withdrawn: 'muted',
  Scheduled: 'booked',
  InProgress: 'pending',
  Approved: 'success',
}

export function StatusBadge({ status }: { status: string }) {
  const tone = TONES[status] ?? 'muted'
  return <span className={`badge badge-${tone}`}>{statusLabel(status)}</span>
}

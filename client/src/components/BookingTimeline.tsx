import type { BookingStatus } from '../api/types'
import { statusLabel } from '../utils/format'

const STEPS: BookingStatus[] = ['Pending', 'Scheduled', 'InProgress', 'Completed']

export function BookingTimeline({ status }: { status: BookingStatus }) {
  const declined = status === 'Declined'
  const cancelled = status === 'Cancelled'
  const ended = declined || cancelled
  const currentIndex = ended ? -1 : STEPS.indexOf(status)

  return (
    <ol className={ended ? 'timeline timeline-cancelled' : 'timeline'}>
      {STEPS.map((step, index) => {
        const state = ended
          ? 'is-idle'
          : index < currentIndex
            ? 'is-done'
            : index === currentIndex
              ? 'is-current'
              : 'is-idle'
        return (
          <li key={step} className={`timeline-step ${state}`}>
            <span className="timeline-dot" aria-hidden="true" />
            <span>{statusLabel(step)}</span>
          </li>
        )
      })}
      {declined ? (
        <li className="timeline-step is-cancelled">
          <span className="timeline-dot" aria-hidden="true" />
          <span>Declined</span>
        </li>
      ) : null}
      {cancelled ? (
        <li className="timeline-step is-cancelled">
          <span className="timeline-dot" aria-hidden="true" />
          <span>Cancelled</span>
        </li>
      ) : null}
    </ol>
  )
}

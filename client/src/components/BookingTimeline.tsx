import type { BookingStatus } from '../api/types'
import { statusLabel } from '../utils/format'

const STEPS: BookingStatus[] = ['Scheduled', 'InProgress', 'Completed']

export function BookingTimeline({ status }: { status: BookingStatus }) {
  const cancelled = status === 'Cancelled'
  const currentIndex = cancelled ? -1 : STEPS.indexOf(status)

  return (
    <ol className={cancelled ? 'timeline timeline-cancelled' : 'timeline'}>
      {STEPS.map((step, index) => {
        const state =
          cancelled
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
      {cancelled ? (
        <li className="timeline-step is-cancelled">
          <span className="timeline-dot" aria-hidden="true" />
          <span>Cancelled</span>
        </li>
      ) : null}
    </ol>
  )
}

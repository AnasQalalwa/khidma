import type { AdminOverview } from '../../api/types'
import { formatCount, formatPercent } from '../../utils/format'

const STEPS: { key: keyof AdminOverview['funnel']; label: string }[] = [
  { key: 'requestsCreated', label: 'Requests' },
  { key: 'requestsWithOffer', label: 'With offer' },
  { key: 'booked', label: 'Booked' },
  { key: 'completed', label: 'Completed' },
]

export function FunnelBars({ funnel }: { funnel: AdminOverview['funnel'] }) {
  const max = Math.max(funnel.requestsCreated, 1)
  return (
    <div aria-label="Marketplace funnel">
      <title>Marketplace funnel</title>
      {STEPS.map((step, index) => {
        const value = funnel[step.key]
        const previousStep = STEPS[index - 1]
        const previous = index === 0 || !previousStep ? value : funnel[previousStep.key]
        const drop = index === 0 || previous === 0 ? null : (previous - value) / previous
        return (
          <div key={step.key}>
            <div className="funnel-row">
              <span className="funnel-label">{step.label}</span>
              <div className="funnel-track">
                <div className="funnel-fill" style={{ width: `${Math.min(100, (value / max) * 100)}%` }} />
              </div>
              <span className="funnel-count">{formatCount(value)}</span>
            </div>
            {drop !== null ? (
              <p className="funnel-drop">
                {drop === 0 ? 'No drop-off' : `${formatPercent(drop)} drop-off`}
              </p>
            ) : null}
          </div>
        )
      })}
    </div>
  )
}

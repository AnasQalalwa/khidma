import type { AdminOverviewRange } from '../../api/types'

const OPTIONS: { value: AdminOverviewRange; label: string }[] = [
  { value: 'today', label: 'Today' },
  { value: '7d', label: '7 days' },
  { value: '30d', label: '30 days' },
]

export function RangeControl({
  value,
  onChange,
}: {
  value: AdminOverviewRange
  onChange: (range: AdminOverviewRange) => void
}) {
  return (
    <div className="range-control" role="radiogroup" aria-label="Date range">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          role="radio"
          aria-checked={value === option.value}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

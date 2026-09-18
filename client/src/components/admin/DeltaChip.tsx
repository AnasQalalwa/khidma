import { Minus, TrendingDown, TrendingUp } from 'lucide-react'
import { formatDelta, type DeltaKind } from '../../utils/format'

export type DeltaTone = 'up' | 'down' | 'neutral'

function deltaTone(value: number, previous: number | null): DeltaTone {
  if (previous === null || value === previous) {
    return 'neutral'
  }

  return value > previous ? 'up' : 'down'
}

export function DeltaChip({
  value,
  previous,
  kind,
}: {
  value: number
  previous: number | null
  kind: DeltaKind
}) {
  const tone = deltaTone(value, previous)
  const Icon = tone === 'up' ? TrendingUp : tone === 'down' ? TrendingDown : Minus
  const label =
    previous === null
      ? 'No comparison'
      : value === previous
        ? 'No change'
        : formatDelta(value - previous, kind)
  const direction =
    tone === 'up' ? 'increased' : tone === 'down' ? 'decreased' : 'unchanged'

  return (
    <span className={`delta-chip delta-chip-${tone}`}>
      <Icon size={14} aria-hidden="true" />
      <span>
        {label}
        <span className="sr-only">, {direction}</span>
      </span>
    </span>
  )
}

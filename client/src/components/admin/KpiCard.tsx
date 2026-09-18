import type { Kpi } from '../../api/types'
import type { DeltaKind } from '../../utils/format'
import { DeltaChip } from './DeltaChip'
import { Sparkline } from './Sparkline'

export function KpiCard({
  label,
  kpi,
  format,
  deltaKind,
  sparkLabel,
}: {
  label: string
  kpi: Kpi
  format: (value: number) => string
  deltaKind: DeltaKind
  sparkLabel: string
}) {
  return (
    <article className="admin-card">
      <div className="admin-kpi-head">
        <p className="admin-card-label">{label}</p>
        <DeltaChip value={kpi.value} previous={kpi.previous} kind={deltaKind} />
      </div>
      <p className="admin-kpi-value">{format(kpi.value)}</p>
      <Sparkline series={kpi.series} label={sparkLabel} />
    </article>
  )
}

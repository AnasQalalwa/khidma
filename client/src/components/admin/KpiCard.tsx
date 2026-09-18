import type { Kpi } from '../../api/types'
import { DeltaChip } from './DeltaChip'
import { Sparkline } from './Sparkline'

export function KpiCard({
  label,
  kpi,
  format,
  sparkLabel,
}: {
  label: string
  kpi: Kpi
  format: (value: number) => string
  sparkLabel: string
}) {
  return (
    <article className="admin-card">
      <div className="admin-kpi-head">
        <p className="admin-card-label">{label}</p>
        <DeltaChip value={kpi.value} previous={kpi.previous} format={format} />
      </div>
      <p className="admin-kpi-value">{format(kpi.value)}</p>
      <Sparkline series={kpi.series} label={sparkLabel} />
    </article>
  )
}

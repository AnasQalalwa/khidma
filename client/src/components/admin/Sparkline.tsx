import { Line, LineChart, ResponsiveContainer } from 'recharts'
import type { SeriesPoint } from '../../api/types'
import { chartColors } from './chartTheme'

export function Sparkline({
  series,
  label,
}: {
  series: SeriesPoint[]
  label: string
}) {
  const colors = chartColors()
  return (
    <div className="sparkline-frame" role="img" aria-label={label}>
      <title>{label}</title>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={series} margin={{ top: 4, right: 0, left: 0, bottom: 0 }}>
          <Line
            type="monotone"
            dataKey="value"
            stroke={colors.primary}
            strokeWidth={2}
            dot={false}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}

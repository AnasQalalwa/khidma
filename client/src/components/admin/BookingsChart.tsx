import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { BookingsSeriesPoint } from '../../api/types'
import { chartColors } from './chartTheme'

function tickLabel(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(date)
}

export function BookingsChart({ series }: { series: BookingsSeriesPoint[] }) {
  const colors = chartColors()
  return (
    <div className="chart-frame" role="img" aria-label="Bookings created, completed, and cancelled over time">
      <title>Bookings over time</title>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={series} margin={{ top: 8, right: 8, left: 0, bottom: 0 }} barGap={2} barCategoryGap="22%">
          <CartesianGrid stroke={colors.border} vertical={false} />
          <XAxis
            dataKey="date"
            tickFormatter={tickLabel}
            tick={{ fill: colors.muted, fontSize: 12 }}
            axisLine={{ stroke: colors.border }}
          />
          <YAxis
            allowDecimals={false}
            tick={{ fill: colors.muted, fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={28}
          />
          <Tooltip
            cursor={{ fill: 'rgba(18, 32, 58, 0.04)' }}
            labelFormatter={(label) => tickLabel(String(label))}
          />
          <Bar dataKey="created" name="Created" fill={colors.primary} radius={[3, 3, 0, 0]} isAnimationActive={false} />
          <Bar dataKey="completed" name="Completed" fill={colors.heading} radius={[3, 3, 0, 0]} isAnimationActive={false} />
          <Bar dataKey="cancelled" name="Cancelled" fill={colors.danger} radius={[3, 3, 0, 0]} isAnimationActive={false} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

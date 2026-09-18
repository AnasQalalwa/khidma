import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
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
        <LineChart data={series} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
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
          <Tooltip labelFormatter={(label) => tickLabel(String(label))} />
          <Legend />
          <Line type="monotone" dataKey="created" name="Created" stroke={colors.primary} strokeWidth={2} dot={false} isAnimationActive={false} />
          <Line type="monotone" dataKey="completed" name="Completed" stroke={colors.heading} strokeWidth={2} dot={false} isAnimationActive={false} />
          <Line type="monotone" dataKey="cancelled" name="Cancelled" stroke={colors.danger} strokeWidth={2} dot={false} isAnimationActive={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}

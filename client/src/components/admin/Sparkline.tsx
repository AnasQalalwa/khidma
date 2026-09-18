import { Line, LineChart, ResponsiveContainer } from 'recharts'
import type { SeriesPoint } from '../../api/types'
import { chartColors } from './chartTheme'

function baselineSeries(series: SeriesPoint[]): SeriesPoint[] {
  if (series.length >= 2) {
    return [
      { date: series[0].date, value: 0 },
      { date: series[series.length - 1].date, value: 0 },
    ]
  }

  if (series.length === 1) {
    return [
      { date: series[0].date, value: 0 },
      { date: `${series[0].date}-end`, value: 0 },
    ]
  }

  return [
    { date: 'start', value: 0 },
    { date: 'end', value: 0 },
  ]
}

function NonZeroDot({
  cx,
  cy,
  payload,
  fill,
}: {
  cx?: number
  cy?: number
  payload?: { value?: number }
  fill?: string
}) {
  if (cx == null || cy == null || !payload?.value) {
    return null
  }

  return <circle cx={cx} cy={cy} r={2.25} fill={fill} />
}

export function Sparkline({
  series,
  label,
}: {
  series: SeriesPoint[]
  label: string
}) {
  const colors = chartColors()
  const nonZeroCount = series.filter((point) => point.value !== 0).length
  const sparse = nonZeroCount < 2
  const data = sparse ? baselineSeries(series) : series

  return (
    <div className={sparse ? 'sparkline sparkline-empty' : 'sparkline'}>
      <div className="sparkline-frame" role="img" aria-label={label}>
        <title>{label}</title>
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data} margin={{ top: 4, right: 2, left: 2, bottom: 0 }}>
            <Line
              type="linear"
              dataKey="value"
              stroke={colors.primary}
              strokeWidth={2}
              dot={
                sparse
                  ? false
                  : (props: { cx?: number; cy?: number; payload?: { value?: number } }) => (
                      <NonZeroDot
                        cx={props.cx}
                        cy={props.cy}
                        payload={props.payload}
                        fill={colors.primary}
                      />
                    )
              }
              activeDot={false}
              isAnimationActive={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
      {sparse ? <p className="muted sparkline-empty-label">Not enough data</p> : null}
    </div>
  )
}

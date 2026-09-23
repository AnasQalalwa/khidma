import { Link } from 'react-router-dom'
import type { ScheduleEntry, WorkingHour } from '../../api/types'
import {
  DAY_SHORT,
  addDays,
  formatHourLabel,
  hoursForDay,
  startOfWeek,
  toDateInput,
} from '../../utils/hours'
import { Button } from '../Button'

const VISIBLE = Array.from({ length: 16 }, (_, index) => index + 7)

function blockSpan(
  item: ScheduleEntry,
  day: Date,
): { column: number; span: number } | null {
  if (!item.start || !item.end || item.status === 'Pending') {
    return null
  }

  const start = new Date(item.start)
  const end = new Date(item.end)
  const visibleStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), VISIBLE[0])
  const visibleEnd = new Date(
    day.getFullYear(),
    day.getMonth(),
    day.getDate(),
    VISIBLE[VISIBLE.length - 1] + 1,
  )
  if (end <= visibleStart || start >= visibleEnd) {
    return null
  }

  const from = start < visibleStart ? VISIBLE[0] : start.getHours()
  const endsOnHour = end.getMinutes() === 0 && end.getSeconds() === 0 && end.getMilliseconds() === 0
  const to = end > visibleEnd ? VISIBLE[VISIBLE.length - 1] + 1 : end.getHours() + (endsOnHour ? 0 : 1)
  const span = to - from
  if (span < 1) {
    return null
  }

  return { column: from - VISIBLE[0] + 1, span }
}

export function WeekScheduleGrid({
  anchor,
  hours,
  items,
  onAnchorChange,
}: {
  anchor: Date
  hours: WorkingHour[]
  items: ScheduleEntry[]
  onAnchorChange: (date: Date) => void
}) {
  const weekStart = startOfWeek(anchor)
  const days = Array.from({ length: 7 }, (_, index) => addDays(weekStart, index))
  const weekEnd = addDays(weekStart, 6)

  return (
    <div className="week-schedule">
      <div className="week-schedule-nav">
        <Button type="button" variant="secondary" size="sm" onClick={() => onAnchorChange(addDays(weekStart, -7))}>
          Previous week
        </Button>
        <strong>
          {weekStart.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} –{' '}
          {weekEnd.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
        </strong>
        <Button type="button" variant="secondary" size="sm" onClick={() => onAnchorChange(addDays(weekStart, 7))}>
          Next week
        </Button>
      </div>
      <div className="week-board">
        <div className="week-axis">
          <div className="hours-grid-corner" />
          <div className="week-hours">
            {VISIBLE.map((hour) => (
              <div key={hour} className="week-hour-head">
                {formatHourLabel(hour)}
              </div>
            ))}
          </div>
        </div>
        {days.map((day) => {
          const key = toDateInput(day)
          const pending = items.filter(
            (item) => item.status === 'Pending' && item.requestedDate?.slice(0, 10) === key,
          )
          const blocks = items.flatMap((item) => {
            const span = blockSpan(item, day)
            return span ? [{ item, ...span }] : []
          })
          return (
            <div key={key} className="week-day-row">
              <div className="week-day-label">
                <span>{DAY_SHORT[day.getDay()]}</span>
                <strong>{day.getDate()}</strong>
                {pending.map((item) => (
                  <Link key={item.bookingId} to={`/provider/bookings/${item.bookingId}`} className="week-pending">
                    {item.serviceName}
                  </Link>
                ))}
              </div>
              <div className="week-day-track">
                {VISIBLE.map((hour) => {
                  const open = hoursForDay(hours, day.getDay()).includes(hour)
                  return <div key={hour} className={open ? 'week-cell' : 'week-cell is-closed'} />
                })}
                {blocks.map(({ item, column, span }) => (
                  <Link
                    key={item.bookingId}
                    to={`/provider/bookings/${item.bookingId}`}
                    className={`week-block week-block-${item.status.toLowerCase()}`}
                    style={{ gridColumn: `${column} / span ${span}` }}
                  >
                    <strong>{item.serviceName}</strong>
                    <span>
                      {item.customerName}
                      {item.start && item.end
                        ? ` · ${formatHourLabel(new Date(item.start).getHours())}–${formatHourLabel(new Date(item.end).getHours())}`
                        : ''}
                    </span>
                  </Link>
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

import { useMemo, useState } from 'react'
import type { BusyInterval, WorkingHour } from '../../api/types'
import { addDays, freeHoursOnDate, parseDateInput, toDateInput } from '../../utils/hours'

const WEEKDAYS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']

export function DayPicker({
  value,
  hours,
  busy,
  onChange,
}: {
  value: string
  hours: WorkingHour[] | null
  busy: BusyInterval[]
  onChange: (value: string) => void
}) {
  const selected = value ? parseDateInput(value) : new Date()
  const [cursor, setCursor] = useState(() => new Date(selected.getFullYear(), selected.getMonth(), 1))
  const today = useMemo(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), now.getDate())
  }, [])

  const first = new Date(cursor.getFullYear(), cursor.getMonth(), 1)
  const start = addDays(first, -first.getDay())
  const cells = Array.from({ length: 42 }, (_, index) => addDays(start, index))

  return (
    <div className="day-picker">
      <div className="day-picker-nav">
        <button type="button" onClick={() => setCursor(new Date(cursor.getFullYear(), cursor.getMonth() - 1, 1))}>
          Previous month
        </button>
        <strong>
          {cursor.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}
        </strong>
        <button type="button" onClick={() => setCursor(new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1))}>
          Next month
        </button>
      </div>
      <div className="day-picker-grid" role="grid" aria-label="Choose a day">
        {WEEKDAYS.map((day) => (
          <div key={day} className="day-picker-weekday">
            {day}
          </div>
        ))}
        {cells.map((date) => {
          const iso = toDateInput(date)
          const inMonth = date.getMonth() === cursor.getMonth()
          const past = date < today
          const closed = hours !== null && freeHoursOnDate(hours, [], date).open === 0
          const disabled = past || closed || !inMonth
          const stats = hours && inMonth && !past && !closed ? freeHoursOnDate(hours, busy, date) : null
          return (
            <button
              key={iso}
              type="button"
              role="gridcell"
              className={[
                'day-cell',
                value === iso ? 'is-selected' : '',
                !inMonth ? 'is-outside' : '',
                closed && inMonth ? 'is-closed' : '',
              ]
                .filter(Boolean)
                .join(' ')}
              disabled={disabled}
              aria-pressed={value === iso}
              onClick={() => onChange(iso)}
            >
              <span>{date.getDate()}</span>
              {stats ? <small>{stats.free}h free</small> : null}
            </button>
          )
        })}
      </div>
    </div>
  )
}

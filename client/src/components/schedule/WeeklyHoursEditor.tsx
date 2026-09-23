import { useRef, useState } from 'react'
import type { WorkingHour } from '../../api/types'
import { DAY_SHORT, formatHourLabel } from '../../utils/hours'
import { Button } from '../Button'

const ALL_HOURS = Array.from({ length: 24 }, (_, hour) => hour)

function key(day: number, hour: number): string {
  return `${day}-${hour}`
}

function toSet(hours: WorkingHour[]): Set<string> {
  return new Set(hours.map((hour) => key(hour.dayOfWeek, hour.hour)))
}

function fromSet(selected: Set<string>): WorkingHour[] {
  return [...selected]
    .map((value) => {
      const [day, hour] = value.split('-').map(Number)
      return { dayOfWeek: day, hour }
    })
    .sort((left, right) => left.dayOfWeek - right.dayOfWeek || left.hour - right.hour)
}

export function WeeklyHoursEditor({
  hours,
  saving,
  onSave,
}: {
  hours: WorkingHour[]
  saving: boolean
  onSave: (hours: WorkingHour[]) => void
}) {
  const [selected, setSelected] = useState(() => toSet(hours))
  const paint = useRef<boolean | null>(null)

  function apply(day: number, hour: number, value: boolean) {
    setSelected((current) => {
      const next = new Set(current)
      const id = key(day, hour)
      if (value) {
        next.add(id)
      } else {
        next.delete(id)
      }
      return next
    })
  }

  function paintCell(day: number, hour: number) {
    if (paint.current === null) {
      return
    }
    apply(day, hour, paint.current)
  }

  function presetWeekday() {
    const next = new Set<string>()
    for (let day = 0; day <= 4; day += 1) {
      for (let hour = 8; hour <= 15; hour += 1) {
        next.add(key(day, hour))
      }
    }
    setSelected(next)
  }

  function copyMonday() {
    setSelected((current) => {
      const next = new Set<string>()
      for (const hour of ALL_HOURS) {
        if (current.has(key(1, hour))) {
          for (let day = 0; day <= 6; day += 1) {
            next.add(key(day, hour))
          }
        }
      }
      return next
    })
  }

  return (
    <div className="hours-editor">
      <div className="hours-editor-tools">
        <Button type="button" variant="secondary" size="sm" onClick={presetWeekday}>
          Sun–Thu 08:00–16:00
        </Button>
        <Button type="button" variant="ghost" size="sm" onClick={copyMonday}>
          Copy Monday to every day
        </Button>
        <Button type="button" variant="ghost" size="sm" onClick={() => setSelected(new Set())}>
          Clear
        </Button>
        <Button type="button" size="sm" loading={saving} onClick={() => onSave(fromSet(selected))}>
          Save working hours
        </Button>
      </div>
      <div
        className="hours-grid"
        onPointerUp={() => {
          paint.current = null
        }}
        onPointerLeave={() => {
          paint.current = null
        }}
      >
        <div className="hours-grid-corner" />
        {DAY_SHORT.map((day) => (
          <div key={day} className="hours-grid-day">
            {day}
          </div>
        ))}
        {ALL_HOURS.map((hour) => (
          <div key={hour} className="hours-grid-row">
            <div className="hours-grid-label">{formatHourLabel(hour)}</div>
            {DAY_SHORT.map((_, day) => {
              const on = selected.has(key(day, hour))
              return (
                <button
                  key={day}
                  type="button"
                  className={on ? 'hours-cell is-on' : 'hours-cell'}
                  aria-pressed={on}
                  aria-label={`${DAY_SHORT[day]} ${formatHourLabel(hour)}`}
                  onPointerDown={(event) => {
                    event.preventDefault()
                    paint.current = !on
                    apply(day, hour, !on)
                  }}
                  onPointerEnter={() => paintCell(day, hour)}
                />
              )
            })}
          </div>
        ))}
      </div>
      <p className="muted">
        Drag across the cells to paint the hours you usually work. Customers can request those days.
      </p>
    </div>
  )
}

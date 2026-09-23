import type { BusyInterval, WorkingHour } from '../../api/types'
import { formatHourLabel, slotWarning, todayInput } from '../../utils/hours'
import { FormField } from '../FormField'

const HOURS = Array.from({ length: 24 }, (_, hour) => hour)
const DURATIONS = Array.from({ length: 12 }, (_, index) => index + 1)

export function ScheduleFields({
  date,
  startHour,
  durationHours,
  hours,
  busy,
  onDateChange,
  onStartHourChange,
  onDurationChange,
}: {
  date: string
  startHour: number
  durationHours: number
  hours: WorkingHour[] | null
  busy: BusyInterval[]
  onDateChange: (value: string) => void
  onStartHourChange: (value: number) => void
  onDurationChange: (value: number) => void
}) {
  const warning = slotWarning(hours, busy, date, startHour, durationHours)
  const endHour = (startHour + durationHours) % 24
  const endsNextDay = startHour + durationHours >= 24

  return (
    <div className="schedule-fields">
      <FormField label="Date">
        <input type="date" min={todayInput()} value={date} onChange={(event) => onDateChange(event.target.value)} required />
      </FormField>
      <FormField label="Start time">
        <select value={startHour} onChange={(event) => onStartHourChange(Number(event.target.value))}>
          {HOURS.map((hour) => (
            <option key={hour} value={hour}>
              {formatHourLabel(hour)}
            </option>
          ))}
        </select>
      </FormField>
      <FormField label="Duration" hint={`Ends at ${formatHourLabel(endHour)}${endsNextDay ? ' the next day' : ''}.`}>
        <select
          value={durationHours}
          onChange={(event) => onDurationChange(Number(event.target.value))}
        >
          {DURATIONS.map((hoursCount) => (
            <option key={hoursCount} value={hoursCount}>
              {hoursCount} {hoursCount === 1 ? 'hour' : 'hours'}
            </option>
          ))}
        </select>
      </FormField>
      {warning ? <p className="schedule-warning">{warning}</p> : null}
    </div>
  )
}

import type { BusyInterval, WorkingHour } from '../api/types'

export const DAY_NAMES = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
] as const

export const DAY_SHORT = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'] as const

export function pad(value: number): string {
  return String(value).padStart(2, '0')
}

export function formatHourLabel(hour: number): string {
  return `${pad(hour)}:00`
}

export function toDateInput(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function parseDateInput(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, (month ?? 1) - 1, day ?? 1)
}

export function addDays(date: Date, days: number): Date {
  const next = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  next.setDate(next.getDate() + days)
  return next
}

export function startOfWeek(date: Date): Date {
  const next = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  next.setDate(next.getDate() - next.getDay())
  return next
}

export function todayInput(): string {
  return toDateInput(new Date())
}

export function hoursForDay(hours: WorkingHour[], dayOfWeek: number): number[] {
  return hours
    .filter((hour) => hour.dayOfWeek === dayOfWeek)
    .map((hour) => hour.hour)
    .sort((left, right) => left - right)
}

function rangesForDay(hours: number[]): string {
  if (hours.length === 0) {
    return ''
  }

  const parts: string[] = []
  let start = hours[0]
  let previous = hours[0]
  for (const hour of hours.slice(1)) {
    if (hour === previous + 1) {
      previous = hour
      continue
    }

    parts.push(`${formatHourLabel(start)}–${formatHourLabel(previous + 1)}`)
    start = hour
    previous = hour
  }

  parts.push(`${formatHourLabel(start)}–${formatHourLabel(previous + 1)}`)
  return parts.join(', ')
}

export function summarizeWorkingHours(hours: WorkingHour[]): string {
  if (hours.length === 0) {
    return 'Hours not set yet'
  }

  const byDay = DAY_SHORT.map((_, day) => rangesForDay(hoursForDay(hours, day)))
  const groups: string[] = []
  let index = 0
  while (index < byDay.length) {
    if (!byDay[index]) {
      index += 1
      continue
    }

    const label = byDay[index]
    let end = index
    while (end + 1 < byDay.length && byDay[end + 1] === label) {
      end += 1
    }

    const days = index === end ? DAY_SHORT[index] : `${DAY_SHORT[index]}–${DAY_SHORT[end]}`
    groups.push(`${days} ${label}`)
    index = end + 1
  }

  return groups.join(' · ')
}

export function localSlot(date: string, startHour: number, durationHours: number): {
  start: Date
  end: Date
} {
  const start = parseDateInput(date)
  start.setHours(startHour, 0, 0, 0)
  const end = new Date(start.getTime() + durationHours * 60 * 60 * 1000)
  return { start, end }
}

export function slotWarning(
  hours: WorkingHour[] | null,
  busy: BusyInterval[],
  date: string,
  startHour: number,
  durationHours: number,
): string | null {
  if (!date || durationHours < 1) {
    return null
  }

  const { start, end } = localSlot(date, startHour, durationHours)
  if (end.getTime() <= Date.now()) {
    return 'Choose a time that has not already ended.'
  }

  if (hours) {
    const cursor = new Date(start)
    let outside = false
    while (cursor < end) {
      const open = hours.some(
        (hour) => hour.dayOfWeek === cursor.getDay() && hour.hour === cursor.getHours(),
      )
      if (!open) {
        outside = true
        break
      }
      cursor.setHours(cursor.getHours() + 1)
    }

    if (outside) {
      return 'This sits outside the usual working hours. You can still book it if you agreed on the call.'
    }
  }

  const clash = busy.some((interval) => {
    const busyStart = new Date(interval.start).getTime()
    const busyEnd = new Date(interval.end).getTime()
    return start.getTime() < busyEnd && end.getTime() > busyStart
  })
  if (clash) {
    return 'This overlaps another scheduled job.'
  }

  return null
}

export function freeHoursOnDate(
  hours: WorkingHour[],
  busy: BusyInterval[],
  date: Date,
): { open: number; free: number } {
  const openHours = hoursForDay(hours, date.getDay())
  if (openHours.length === 0) {
    return { open: 0, free: 0 }
  }

  const free = openHours.filter((hour) => {
    const start = new Date(date.getFullYear(), date.getMonth(), date.getDate(), hour)
    const end = new Date(start.getTime() + 60 * 60 * 1000)
    return !busy.some((interval) => {
      const busyStart = new Date(interval.start).getTime()
      const busyEnd = new Date(interval.end).getTime()
      return start.getTime() < busyEnd && end.getTime() > busyStart
    })
  }).length

  return { open: openHours.length, free }
}

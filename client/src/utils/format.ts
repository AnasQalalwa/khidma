export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

export function formatMoney(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '—'
  }

  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatBudget(
  min: number | null | undefined,
  max: number | null | undefined,
): string {
  if (min == null && max == null) {
    return 'Flexible'
  }

  if (min != null && max != null) {
    return `${formatMoney(min)} – ${formatMoney(max)}`
  }

  if (min != null) {
    return `From ${formatMoney(min)}`
  }

  return `Up to ${formatMoney(max)}`
}

export function formatRating(value: number, count: number): string {
  if (count === 0) {
    return 'No reviews yet'
  }

  return `${value.toFixed(2)} · ${count} ${count === 1 ? 'review' : 'reviews'}`
}

export function toDateTimeLocal(value: string | Date): string {
  const date = typeof value === 'string' ? new Date(value) : value
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function fromDateTimeLocal(value: string): string {
  return new Date(value).toISOString()
}

export function futureDateTimeLocal(daysAhead = 7): string {
  const date = new Date()
  date.setDate(date.getDate() + daysAhead)
  date.setMinutes(0, 0, 0)
  return toDateTimeLocal(date)
}

export function statusLabel(status: string): string {
  if (status === 'InProgress') {
    return 'In Progress'
  }

  return status
}

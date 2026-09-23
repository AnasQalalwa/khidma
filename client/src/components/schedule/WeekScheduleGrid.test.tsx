import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import type { ScheduleEntry } from '../../api/types'
import { WeekScheduleGrid } from './WeekScheduleGrid'

const carpet: ScheduleEntry = {
  bookingId: 9,
  serviceName: 'Carpet Cleaning',
  customerName: 'Anas mq',
  status: 'Scheduled',
  start: new Date(2026, 8, 25, 10).toISOString(),
  end: new Date(2026, 8, 25, 16).toISOString(),
  requestedDate: '2026-09-25',
}

describe('WeekScheduleGrid', () => {
  it('draws a multi-hour job as one slot across the time columns', () => {
    render(
      <MemoryRouter>
        <WeekScheduleGrid
          anchor={new Date(2026, 8, 25, 12)}
          hours={[{ dayOfWeek: 5, hour: 10 }]}
          items={[carpet]}
          onAnchorChange={() => undefined}
        />
      </MemoryRouter>,
    )

    const slots = screen.getAllByRole('link', { name: /Carpet Cleaning/ })
    expect(slots).toHaveLength(1)
    expect(slots[0]).toHaveStyle({ gridColumn: '4 / span 6' })
    expect(screen.getByText('Fri')).toBeInTheDocument()
    expect(screen.getByText('07:00')).toBeInTheDocument()
  })
})

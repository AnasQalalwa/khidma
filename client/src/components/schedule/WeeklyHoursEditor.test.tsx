import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { WeeklyHoursEditor } from './WeeklyHoursEditor'

describe('WeeklyHoursEditor', () => {
  it('paints a cell and saves the selected hour', async () => {
    const onSave = vi.fn()
    const user = userEvent.setup()
    render(<WeeklyHoursEditor hours={[]} saving={false} onSave={onSave} />)

    const cell = screen.getByRole('button', { name: 'Mon 09:00' })
    await user.click(cell)
    expect(cell).toHaveAttribute('aria-pressed', 'true')
    await user.click(screen.getByRole('button', { name: 'Save working hours' }))
    expect(onSave).toHaveBeenCalledWith([{ dayOfWeek: 1, hour: 9 }])
  })
})

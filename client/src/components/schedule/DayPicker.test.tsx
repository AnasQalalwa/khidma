import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DayPicker } from './DayPicker'
import { addDays, toDateInput } from '../../utils/hours'

describe('DayPicker', () => {
  it('selects an open future day and skips closed weekdays', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    const target = addDays(new Date(), 3)
    render(
      <DayPicker
        value=""
        hours={[{ dayOfWeek: target.getDay(), hour: 9 }]}
        busy={[]}
        onChange={onChange}
      />,
    )

    const shown = screen.getByText(
      new Date().toLocaleDateString(undefined, { month: 'long', year: 'numeric' }),
    )
    if (target.getMonth() !== new Date().getMonth()) {
      await user.click(screen.getByRole('button', { name: 'Next month' }))
      expect(shown).not.toBeInTheDocument()
    }

    const closed = screen
      .getAllByRole('gridcell')
      .find((cell) => cell.className.includes('is-closed') && !cell.className.includes('is-outside'))
    expect(closed).toBeDisabled()

    const open = screen.getAllByRole('gridcell').find((cell) => {
      return cell.querySelector('span')?.textContent === String(target.getDate()) && !cell.hasAttribute('disabled')
    })
    expect(open).toBeDefined()
    await user.click(open!)
    expect(onChange).toHaveBeenCalledWith(toDateInput(target))
  })
})

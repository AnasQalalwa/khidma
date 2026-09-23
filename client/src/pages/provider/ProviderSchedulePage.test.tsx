import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { getMySchedule } from '../../api/schedule'
import { AuthContext } from '../../auth/AuthContext'
import { providerUser, createAuthValue } from '../../test/render'
import { ProviderSchedulePage } from './ProviderSchedulePage'

vi.mock('../../api/schedule', () => ({
  getMySchedule: vi.fn(),
  saveMyWorkingHours: vi.fn(),
}))

const mockedGetMySchedule = vi.mocked(getMySchedule)

describe('ProviderSchedulePage', () => {
  beforeEach(() => {
    mockedGetMySchedule.mockReset()
    mockedGetMySchedule.mockResolvedValue({
      workingHours: [{ dayOfWeek: 1, hour: 9 }],
      items: [],
    })
  })

  it('shows the week schedule and the working-hours editor', async () => {
    render(
      <MemoryRouter>
        <AuthContext.Provider value={createAuthValue({ user: providerUser(), authenticated: true })}>
          <ProviderSchedulePage />
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Schedule' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next week' })).toBeInTheDocument()
    await (await import('@testing-library/user-event')).default.setup().click(
      screen.getByRole('tab', { name: 'Working hours' }),
    )
    expect(screen.getByRole('button', { name: 'Save working hours' })).toBeInTheDocument()
  })
})

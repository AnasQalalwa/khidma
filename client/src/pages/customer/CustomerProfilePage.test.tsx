import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAccountProfile, updateAccountProfile } from '../../api/account'
import { customerUser, renderWithRouter } from '../../test/render'
import { CustomerProfilePage } from './CustomerProfilePage'

vi.mock('../../api/account', () => ({
  getAccountProfile: vi.fn(),
  updateAccountProfile: vi.fn(),
  changePassword: vi.fn(),
}))

const mockedGetAccountProfile = vi.mocked(getAccountProfile)
const mockedUpdateAccountProfile = vi.mocked(updateAccountProfile)

describe('CustomerProfilePage', () => {
  beforeEach(() => {
    mockedGetAccountProfile.mockReset()
    mockedUpdateAccountProfile.mockReset()
    mockedGetAccountProfile.mockResolvedValue({
      fullName: 'Test Customer',
      email: 'customer@khidma.test',
      phoneNumber: '0591111111',
      role: 'Customer',
      city: 'Ramallah',
    })
  })

  it('saves the customer phone and city', async () => {
    mockedUpdateAccountProfile.mockResolvedValue({
      fullName: 'Test Customer',
      email: 'customer@khidma.test',
      phoneNumber: '+970 0599999999',
      role: 'Customer',
      city: 'Nablus',
    })
    const refreshUser = vi.fn()
    const user = userEvent.setup()

    renderWithRouter(<CustomerProfilePage />, {
      route: '/account',
      path: '/account',
      auth: { user: customerUser(), authenticated: true, refreshUser },
    })

    expect(await screen.findByDisplayValue('0591111111')).toBeInTheDocument()
    await user.clear(screen.getByLabelText('Phone number'))
    await user.type(screen.getByLabelText('Phone number'), '0599999999')
    await user.selectOptions(screen.getByLabelText('City'), 'Nablus')
    await user.click(screen.getByRole('button', { name: 'Save profile' }))

    expect(mockedUpdateAccountProfile).toHaveBeenCalledWith({
      fullName: 'Test Customer',
      phoneNumber: '+970 0599999999',
      city: 'Nablus',
    })
    expect(await screen.findByText('Profile saved.')).toBeInTheDocument()
    expect(refreshUser).toHaveBeenCalled()
  })
})

import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getProviderDashboard } from '../api/dashboard'
import { providerUser, renderWithRouter } from '../test/render'
import { ProviderDashboard } from './ProviderDashboard'

vi.mock('../api/dashboard', () => ({
  getProviderDashboard: vi.fn(),
}))

const mockedGetProviderDashboard = vi.mocked(getProviderDashboard)

describe('ProviderDashboard', () => {
  beforeEach(() => {
    mockedGetProviderDashboard.mockReset()
  })

  it('shows a suspended banner with the admin reason', async () => {
    mockedGetProviderDashboard.mockResolvedValue({
      verificationStatus: 'Approved',
      isSuspended: true,
      suspensionReason: 'Repeated no-shows',
      eligibleRequestCount: 0,
      pendingOfferCount: 0,
      activeBookingCount: 1,
      averageRating: 4.2,
      reviewCount: 3,
      recentAvailableRequests: [],
      recentOffers: [],
      activeJobs: [],
    })

    renderWithRouter(<ProviderDashboard />, {
      route: '/provider',
      path: '/provider',
      auth: { user: providerUser(), authenticated: true },
    })

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('Your account is suspended')
    expect(alert).toHaveTextContent('Repeated no-shows')
  })
})

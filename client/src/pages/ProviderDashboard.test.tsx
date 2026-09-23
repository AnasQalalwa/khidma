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
      pendingRequestCount: 0,
      activeJobCount: 1,
      averageRating: 4.2,
      reviewCount: 3,
      recentPendingRequests: [],
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

  it('points pending providers to profile document upload', async () => {
    mockedGetProviderDashboard.mockResolvedValue({
      verificationStatus: 'PendingReview',
      isSuspended: false,
      suspensionReason: null,
      pendingRequestCount: 0,
      activeJobCount: 0,
      averageRating: 0,
      reviewCount: 0,
      recentPendingRequests: [],
      activeJobs: [],
    })

    renderWithRouter(<ProviderDashboard />, {
      route: '/provider',
      path: '/provider',
      auth: { user: providerUser(), authenticated: true },
    })

    expect(
      await screen.findByRole('link', { name: 'Profile → Professional verification' }),
    ).toHaveAttribute('href', '/provider/profile#verification')
  })
})

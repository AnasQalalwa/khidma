import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAdminProviders } from '../../api/admin'
import { setProviderSuspension } from '../../api/verification'
import type { AdminProvider } from '../../api/types'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminProvidersPage } from './AdminProvidersPage'

vi.mock('../../api/admin', () => ({
  getAdminProviders: vi.fn(),
}))

vi.mock('../../api/verification', () => ({
  setProviderSuspension: vi.fn(),
}))

const mockedGetAdminProviders = vi.mocked(getAdminProviders)
const mockedSetProviderSuspension = vi.mocked(setProviderSuspension)

function provider(overrides: Partial<AdminProvider> = {}): AdminProvider {
  return {
    id: 4,
    userId: 'provider-1',
    fullName: 'Sami Provider',
    email: 'provider@khidma.test',
    city: 'Ramallah',
    verificationStatus: 'Approved',
    isSuspended: false,
    suspensionReason: null,
    averageRating: 4.5,
    reviewCount: 2,
    documentCount: 1,
    approvedDocumentCount: 1,
    services: ['Plumbing'],
    ...overrides,
  }
}

function pageOf(items: AdminProvider[]) {
  return {
    items,
    page: 1,
    pageSize: 12,
    totalCount: items.length,
    totalPages: 1,
  }
}

describe('AdminProvidersPage', () => {
  beforeEach(() => {
    mockedGetAdminProviders.mockReset()
    mockedSetProviderSuspension.mockReset()
  })

  it('requires a reason before suspending a provider', async () => {
    mockedGetAdminProviders.mockResolvedValue(pageOf([provider()]))
    const user = userEvent.setup()

    renderWithRouter(<AdminProvidersPage />, {
      route: '/admin/providers',
      path: '/admin/providers',
      auth: { user: adminUser(), authenticated: true },
    })

    await user.click((await screen.findAllByRole('button', { name: 'Suspend' }))[0])
    await user.click(screen.getByRole('button', { name: 'Suspend provider' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('A reason is required.')
    expect(mockedSetProviderSuspension).not.toHaveBeenCalled()
  })

  it('reactivates a suspended provider after confirmation', async () => {
    mockedGetAdminProviders.mockResolvedValue(
      pageOf([provider({ isSuspended: true, suspensionReason: 'Policy violation' })]),
    )
    mockedSetProviderSuspension.mockResolvedValue({
      providerProfileId: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      city: 'Ramallah',
      yearsOfExperience: 5,
      bio: null,
      verificationStatus: 'Approved',
      verificationRejectionReason: null,
      verificationReviewedAt: null,
      isSuspended: false,
      suspensionReason: null,
      suspendedAt: null,
      averageRating: 4.5,
      reviewCount: 2,
      services: ['Plumbing'],
      documents: [],
      hasApprovedDocument: true,
    })
    const user = userEvent.setup()

    renderWithRouter(<AdminProvidersPage />, {
      route: '/admin/providers',
      path: '/admin/providers',
      auth: { user: adminUser(), authenticated: true },
    })

    await user.click((await screen.findAllByRole('button', { name: 'Reactivate' }))[0])
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Reactivate' }))

    expect(mockedSetProviderSuspension).toHaveBeenCalledWith(4, { suspended: false })
  })
})

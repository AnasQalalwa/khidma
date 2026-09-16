import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/client'
import { getAdminVerifications } from '../../api/verification'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminVerificationsPage } from './AdminVerificationsPage'

vi.mock('../../api/verification', () => ({
  getAdminVerifications: vi.fn(),
}))

const mockedGetAdminVerifications = vi.mocked(getAdminVerifications)

describe('AdminVerificationsPage', () => {
  beforeEach(() => {
    mockedGetAdminVerifications.mockReset()
  })

  it('shows a loading state while the queue is fetched', () => {
    mockedGetAdminVerifications.mockReturnValue(new Promise(() => undefined))

    renderWithRouter(<AdminVerificationsPage />, {
      route: '/admin/verifications',
      path: '/admin/verifications',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(screen.getByText('Loading verifications')).toBeInTheDocument()
  })

  it('shows an empty state when the queue has no providers', async () => {
    mockedGetAdminVerifications.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 12,
      totalCount: 0,
      totalPages: 0,
    })

    renderWithRouter(<AdminVerificationsPage />, {
      route: '/admin/verifications',
      path: '/admin/verifications',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('No providers in this queue')).toBeInTheDocument()
  })

  it('renders an error state from an ApiError', async () => {
    mockedGetAdminVerifications.mockRejectedValue(new ApiError('Forbidden.', 403))

    renderWithRouter(<AdminVerificationsPage />, {
      route: '/admin/verifications',
      path: '/admin/verifications',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('Unable to load verifications')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Forbidden.')
  })

  it('renders providers waiting for review', async () => {
    mockedGetAdminVerifications.mockResolvedValue({
      items: [
        {
          providerProfileId: 4,
          userId: 'provider-1',
          fullName: 'Sami Provider',
          email: 'provider@khidma.test',
          city: 'Ramallah',
          yearsOfExperience: 5,
          verificationStatus: 'PendingReview',
          isSuspended: false,
          documentCount: 1,
          pendingDocumentCount: 1,
          approvedDocumentCount: 0,
          rejectedDocumentCount: 0,
          services: ['Plumbing'],
        },
      ],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      totalPages: 1,
    })

    renderWithRouter(<AdminVerificationsPage />, {
      route: '/admin/verifications',
      path: '/admin/verifications',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('Sami Provider')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Open review' })).toHaveAttribute(
      'href',
      '/admin/verifications/4',
    )
  })
})

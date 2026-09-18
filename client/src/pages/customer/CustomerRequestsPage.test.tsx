import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/client'
import { getMyRequests } from '../../api/requests'
import { customerUser, renderWithRouter } from '../../test/render'
import { CustomerRequestsPage } from './CustomerRequestsPage'

vi.mock('../../api/requests', () => ({
  getMyRequests: vi.fn(),
}))

const mockedGetMyRequests = vi.mocked(getMyRequests)

describe('CustomerRequestsPage', () => {
  beforeEach(() => {
    mockedGetMyRequests.mockReset()
  })

  it('shows an empty state when the customer has no requests', async () => {
    mockedGetMyRequests.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 12,
      totalCount: 0,
      totalPages: 0,
    })

    const user = customerUser()
    renderWithRouter(<CustomerRequestsPage />, {
      route: '/customer/requests',
      path: '/customer/requests',
      auth: { user, authenticated: true },
    })

    expect(
      await screen.findByText('No service requests yet.'),
    ).toBeInTheDocument()
    expect(
      screen.getByText(
        'Create a request to start receiving offers from local providers.',
      ),
    ).toBeInTheDocument()
  })

  it('renders ErrorState from an ApiError', async () => {
    mockedGetMyRequests.mockRejectedValue(
      new ApiError('You do not own this service request.', 403),
    )

    const user = customerUser()
    renderWithRouter(<CustomerRequestsPage />, {
      route: '/customer/requests',
      path: '/customer/requests',
      auth: { user, authenticated: true },
    })

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'You do not own this service request.',
    )
    expect(screen.getByText('Unable to load requests')).toBeInTheDocument()
  })
})

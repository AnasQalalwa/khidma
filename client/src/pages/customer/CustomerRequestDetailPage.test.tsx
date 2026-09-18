import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { acceptOffer } from '../../api/offers'
import { ApiError } from '../../api/client'
import { cancelServiceRequest, getCustomerRequest } from '../../api/requests'
import type { RequestDetailForCustomer } from '../../api/types'
import { customerUser, renderWithRouter } from '../../test/render'
import { CustomerRequestDetailPage } from './CustomerRequestDetailPage'

vi.mock('../../api/offers', () => ({
  acceptOffer: vi.fn(),
}))

vi.mock('../../api/requests', () => ({
  getCustomerRequest: vi.fn(),
  cancelServiceRequest: vi.fn(),
}))

const mockedAcceptOffer = vi.mocked(acceptOffer)
const mockedGetCustomerRequest = vi.mocked(getCustomerRequest)

function requestDetail(
  overrides: Partial<RequestDetailForCustomer> = {},
): RequestDetailForCustomer {
  return {
    id: 12,
    title: 'Kitchen leak',
    description: 'Pipe under the sink is dripping.',
    serviceId: 1,
    serviceName: 'Plumbing',
    categoryName: 'Home',
    city: 'Ramallah',
    preferredDate: '2026-09-20T10:00:00Z',
    budgetMin: 50,
    budgetMax: 120,
    status: 'Open',
    offerCount: 1,
    createdAt: '2026-09-16T08:00:00Z',
    canEdit: true,
    canCancel: true,
    bookingId: null,
    offers: [
      {
        id: 44,
        providerId: 'provider-1',
        providerProfileId: 3,
        providerDisplayName: 'Rami Plumber',
        providerAverageRating: 4.8,
        providerReviewCount: 12,
        price: 90,
        message: 'I can come tomorrow morning.',
        estimatedDate: '2026-09-21T08:00:00Z',
        status: 'Pending',
        createdAt: '2026-09-16T09:00:00Z',
        canAccept: true,
      },
    ],
    ...overrides,
  }
}

describe('CustomerRequestDetailPage', () => {
  beforeEach(() => {
    mockedAcceptOffer.mockReset()
    mockedGetCustomerRequest.mockReset()
    vi.mocked(cancelServiceRequest).mockReset()
    mockedGetCustomerRequest.mockResolvedValue(requestDetail())
  })

  it('shows the server 409 message and reloads offers', async () => {
    mockedAcceptOffer.mockRejectedValue(
      new ApiError('Another offer was accepted first.', 409, {
        title: 'Conflict',
        status: 409,
        detail: 'Another offer was accepted first.',
      }),
    )

    renderWithRouter(<CustomerRequestDetailPage />, {
      route: '/customer/requests/12',
      path: '/customer/requests/:id',
      auth: { user: customerUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Accept offer' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Accept offer' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Another offer was accepted first.',
    )
    expect(screen.getByRole('button', { name: 'Reload offers' })).toBeInTheDocument()

    mockedGetCustomerRequest.mockResolvedValue(
      requestDetail({
        status: 'Booked',
        offerCount: 1,
        bookingId: 8,
        offers: [
          {
            ...requestDetail().offers[0],
            canAccept: false,
            status: 'Rejected',
          },
        ],
      }),
    )

    await user.click(screen.getByRole('button', { name: 'Reload offers' }))

    expect(await screen.findByRole('link', { name: 'View booking' })).toBeInTheDocument()
    expect(mockedGetCustomerRequest).toHaveBeenCalledTimes(2)
  })
})

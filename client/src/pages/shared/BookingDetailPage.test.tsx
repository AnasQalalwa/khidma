import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getBooking } from '../../api/bookings'
import type { BookingDetail } from '../../api/types'
import { providerUser, renderWithRouter } from '../../test/render'
import { BookingDetailPage } from './BookingDetailPage'

vi.mock('../../api/bookings', () => ({
  getBooking: vi.fn(),
  startBooking: vi.fn(),
  completeBooking: vi.fn(),
  cancelBooking: vi.fn(),
}))

const mockedGetBooking = vi.mocked(getBooking)

function scheduledBooking(
  overrides: Partial<BookingDetail> = {},
): BookingDetail {
  return {
    id: 7,
    offerId: 3,
    serviceRequestId: 9,
    providerProfileId: 4,
    title: 'Kitchen leak',
    description: 'Water under the sink.',
    serviceName: 'Plumbing',
    categoryName: 'Home',
    city: 'Ramallah',
    scheduledDate: new Date().toISOString(),
    finalPrice: 90,
    status: 'Scheduled',
    customerId: 'customer-1',
    providerId: 'provider-1',
    customerName: 'Test Customer',
    providerName: 'Test Provider',
    customerEmail: 'customer@khidma.test',
    providerEmail: 'provider@khidma.test',
    customerContact: '0590000000',
    customerCity: 'Ramallah',
    providerCity: 'Ramallah',
    createdAt: new Date().toISOString(),
    startedAt: null,
    completedAt: null,
    cancelledAt: null,
    cancellationReason: null,
    canStart: true,
    canComplete: false,
    canCancel: true,
    canReview: false,
    review: null,
    ...overrides,
  }
}

describe('BookingDetailPage', () => {
  beforeEach(() => {
    mockedGetBooking.mockReset()
  })

  it('renders the timeline and provider actions for a scheduled booking', async () => {
    mockedGetBooking.mockResolvedValue(scheduledBooking())
    const user = providerUser()

    renderWithRouter(<BookingDetailPage role="Provider" />, {
      route: '/provider/bookings/7',
      path: '/provider/bookings/:id',
      auth: { user, authenticated: true },
    })

    expect(await screen.findByText('Kitchen leak')).toBeInTheDocument()
    expect(screen.getAllByText('Scheduled').length).toBeGreaterThan(0)
    expect(screen.getByText('In Progress')).toBeInTheDocument()
    expect(screen.getByText('Completed')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Start work' })).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Cancel booking' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Complete job' }),
    ).not.toBeInTheDocument()
  })

  it('shows complete and hides start when the booking is in progress', async () => {
    mockedGetBooking.mockResolvedValue(
      scheduledBooking({
        status: 'InProgress',
        canStart: false,
        canComplete: true,
        canCancel: false,
        startedAt: new Date().toISOString(),
      }),
    )
    const user = providerUser()

    renderWithRouter(<BookingDetailPage role="Provider" />, {
      route: '/provider/bookings/7',
      path: '/provider/bookings/:id',
      auth: { user, authenticated: true },
    })

    expect(
      await screen.findByRole('button', { name: 'Complete job' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Start work' }),
    ).not.toBeInTheDocument()
  })
})

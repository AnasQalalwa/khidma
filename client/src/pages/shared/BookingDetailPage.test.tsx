import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { acceptBooking, declineBooking, getBooking, rescheduleBooking } from '../../api/bookings'
import type { BookingDetail } from '../../api/types'
import { providerUser, renderWithRouter } from '../../test/render'
import { BookingDetailPage } from './BookingDetailPage'

vi.mock('../../api/bookings', () => ({
  getBooking: vi.fn(),
  startBooking: vi.fn(),
  completeBooking: vi.fn(),
  cancelBooking: vi.fn(),
  acceptBooking: vi.fn(),
  declineBooking: vi.fn(),
  rescheduleBooking: vi.fn(),
}))

const mockedGetBooking = vi.mocked(getBooking)
const mockedAcceptBooking = vi.mocked(acceptBooking)
const mockedDeclineBooking = vi.mocked(declineBooking)
const mockedRescheduleBooking = vi.mocked(rescheduleBooking)

function scheduledBooking(
  overrides: Partial<BookingDetail> = {},
): BookingDetail {
  return {
    id: 7,
    serviceId: 2,
    providerProfileId: 4,
    serviceName: 'Plumbing',
    categoryName: 'Home',
    city: 'Ramallah',
    notes: 'Water under the sink.',
    requestedDate: new Date().toISOString(),
    scheduledStart: null,
    scheduledEnd: null,
    durationHours: null,
    rescheduledAt: null,
    rescheduleNote: null,
    quotedPrice: 90,
    providerMessage: null,
    declineReason: null,
    status: 'Scheduled',
    customerId: 'customer-1',
    providerId: 'provider-1',
    customerName: 'Test Customer',
    providerName: 'Test Provider',
    customerPhone: '0591111111',
    providerPhone: '0593333333',
    customerCity: 'Ramallah',
    providerCity: 'Ramallah',
    createdAt: new Date().toISOString(),
    respondedAt: new Date().toISOString(),
    startedAt: null,
    completedAt: null,
    cancelledAt: null,
    cancellationReason: null,
    canAccept: false,
    canReschedule: false,
    canDecline: false,
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

    expect(await screen.findByRole('heading', { name: 'Plumbing' })).toBeInTheDocument()
    expect(screen.getByText('+970 0591111111')).toBeInTheDocument()
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

  it('lets the provider accept a pending request with a price', async () => {
    mockedGetBooking.mockResolvedValue(
      scheduledBooking({
        status: 'Pending',
        quotedPrice: null,
        respondedAt: null,
        canAccept: true,
        canDecline: true,
        canStart: false,
        canCancel: false,
      }),
    )
    mockedAcceptBooking.mockResolvedValue(scheduledBooking({ quotedPrice: 150 }))
    const user = userEvent.setup()

    renderWithRouter(<BookingDetailPage role="Provider" />, {
      route: '/provider/bookings/7',
      path: '/provider/bookings/:id',
      auth: { user: providerUser(), authenticated: true },
    })

    await user.type(await screen.findByLabelText('Price'), '150')
    await user.type(screen.getByLabelText('Message'), 'Morning works.')
    await user.click(screen.getByRole('button', { name: 'Accept booking' }))

    expect(mockedAcceptBooking).toHaveBeenCalledWith(
      7,
      expect.objectContaining({
        price: 150,
        message: 'Morning works.',
        durationHours: 2,
      }),
    )
  })

  it('lets the provider reschedule a scheduled visit', async () => {
    mockedGetBooking.mockResolvedValue(
      scheduledBooking({
        canReschedule: true,
        scheduledStart: new Date().toISOString(),
        durationHours: 2,
      }),
    )
    mockedRescheduleBooking.mockResolvedValue(
      scheduledBooking({
        canReschedule: true,
        durationHours: 2,
        rescheduleNote: 'Customer asked for later',
        rescheduledAt: new Date().toISOString(),
      }),
    )
    const user = userEvent.setup()

    renderWithRouter(<BookingDetailPage role="Provider" />, {
      route: '/provider/bookings/7',
      path: '/provider/bookings/:id',
      auth: { user: providerUser(), authenticated: true },
    })

    await user.click(await screen.findByRole('button', { name: 'Reschedule' }))
    await user.type(screen.getByLabelText('Note'), 'Customer asked for later')
    await user.click(screen.getByRole('button', { name: 'Save schedule' }))

    expect(mockedRescheduleBooking).toHaveBeenCalledWith(
      7,
      expect.objectContaining({
        durationHours: 2,
        note: 'Customer asked for later',
      }),
    )
    expect(await screen.findByText(/Customer asked for later/)).toBeInTheDocument()
  })

  it('lets the provider decline a pending request', async () => {
    mockedGetBooking.mockResolvedValue(
      scheduledBooking({
        status: 'Pending',
        quotedPrice: null,
        respondedAt: null,
        canAccept: true,
        canDecline: true,
        canStart: false,
        canCancel: false,
      }),
    )
    mockedDeclineBooking.mockResolvedValue(
      scheduledBooking({
        status: 'Declined',
        canAccept: false,
        canDecline: false,
        canStart: false,
        canCancel: false,
        declineReason: 'Not available',
      }),
    )
    const user = userEvent.setup()

    renderWithRouter(<BookingDetailPage role="Provider" />, {
      route: '/provider/bookings/7',
      path: '/provider/bookings/:id',
      auth: { user: providerUser(), authenticated: true },
    })

    await user.click(await screen.findByRole('button', { name: 'Decline' }))
    await user.type(screen.getByLabelText('Reason'), 'Not available')
    await user.click(screen.getByRole('button', { name: 'Decline request' }))
    expect(mockedDeclineBooking).toHaveBeenCalledWith(7, 'Not available')
    expect(await screen.findByText('Not available')).toBeInTheDocument()
  })
})

import { apiRequest } from './client'
import { toQuery } from './query'
import type {
  AcceptBookingPayload,
  BookingDetail,
  BookingSummary,
  CreateBookingPayload,
  ReschedulePayload,
  PageQuery,
  PagedResult,
  Review,
} from './types'

export function createBooking(payload: CreateBookingPayload): Promise<BookingDetail> {
  return apiRequest('/api/bookings', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getMyBookings(
  query: PageQuery = {},
): Promise<PagedResult<BookingSummary>> {
  return apiRequest(`/api/bookings/mine${toQuery(query)}`)
}

export function getBooking(id: number): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}`)
}

export function acceptBooking(
  id: number,
  payload: AcceptBookingPayload,
): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/accept`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function rescheduleBooking(
  id: number,
  payload: ReschedulePayload,
): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/schedule`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function declineBooking(id: number, reason: string): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/decline`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export function startBooking(id: number): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/start`, { method: 'POST' })
}

export function completeBooking(id: number): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/complete`, { method: 'POST' })
}

export function cancelBooking(id: number, reason: string): Promise<BookingDetail> {
  return apiRequest(`/api/bookings/${id}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export function createReview(
  bookingId: number,
  payload: { rating: number; comment?: string },
): Promise<Review> {
  return apiRequest(`/api/bookings/${bookingId}/review`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

import { apiRequest } from './client'
import { toQuery } from './query'
import type {
  BookingDetail,
  OfferForCustomer,
  OfferMine,
  OfferSnapshot,
  PageQuery,
  PagedResult,
  SubmitOfferPayload,
} from './types'

export function submitOffer(
  requestId: number,
  payload: SubmitOfferPayload,
): Promise<OfferSnapshot> {
  return apiRequest(`/api/service-requests/${requestId}/offers`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getRequestOffers(requestId: number): Promise<OfferForCustomer[]> {
  return apiRequest(`/api/service-requests/${requestId}/offers`)
}

export function getMyOffers(query: PageQuery = {}): Promise<PagedResult<OfferMine>> {
  return apiRequest(`/api/offers/mine${toQuery(query)}`)
}

export function withdrawOffer(id: number): Promise<OfferSnapshot> {
  return apiRequest(`/api/offers/${id}/withdraw`, {
    method: 'POST',
  })
}

export function acceptOffer(id: number): Promise<BookingDetail> {
  return apiRequest(`/api/offers/${id}/accept`, {
    method: 'POST',
  })
}

import { apiRequest } from './client'
import { toQuery } from './query'
import type {
  CreateServiceRequestPayload,
  PageQuery,
  PagedResult,
  RequestDetailForCustomer,
  RequestDetailForProvider,
  RequestSummaryForProvider,
  ServiceRequestSummary,
  UpdateServiceRequestPayload,
} from './types'

export function createServiceRequest(
  payload: CreateServiceRequestPayload,
): Promise<RequestDetailForCustomer> {
  return apiRequest<RequestDetailForCustomer>('/api/service-requests', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getMyRequests(
  query: PageQuery = {},
): Promise<PagedResult<ServiceRequestSummary>> {
  return apiRequest(`/api/service-requests/mine${toQuery(query)}`)
}

export function getAvailableRequests(
  query: PageQuery = {},
): Promise<PagedResult<RequestSummaryForProvider>> {
  return apiRequest(`/api/service-requests/available${toQuery(query)}`)
}

export function getCustomerRequest(id: number): Promise<RequestDetailForCustomer> {
  return apiRequest(`/api/service-requests/${id}`)
}

export function getProviderRequest(id: number): Promise<RequestDetailForProvider> {
  return apiRequest(`/api/service-requests/${id}`)
}

export function updateServiceRequest(
  id: number,
  payload: UpdateServiceRequestPayload,
): Promise<RequestDetailForCustomer> {
  return apiRequest(`/api/service-requests/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function cancelServiceRequest(id: number): Promise<RequestDetailForCustomer> {
  return apiRequest(`/api/service-requests/${id}/cancel`, {
    method: 'POST',
  })
}

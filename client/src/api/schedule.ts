import { apiRequest } from './client'
import type { ProviderAvailability, ProviderSchedule, WorkingHour } from './types'

export function getMyWorkingHours(): Promise<{ hours: WorkingHour[] }> {
  return apiRequest('/api/providers/me/working-hours')
}

export function saveMyWorkingHours(hours: WorkingHour[]): Promise<{ hours: WorkingHour[] }> {
  return apiRequest('/api/providers/me/working-hours', {
    method: 'PUT',
    body: JSON.stringify({ hours }),
  })
}

export function getMySchedule(from: string, to: string): Promise<ProviderSchedule> {
  const query = `?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
  return apiRequest(`/api/providers/me/schedule${query}`)
}

export function getProviderAvailability(
  providerId: number,
  from: string,
  to: string,
): Promise<ProviderAvailability> {
  const query = `?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
  return apiRequest(`/api/providers/${providerId}/availability${query}`)
}

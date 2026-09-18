import { apiRequest } from './client'
import type { ProviderMe, PublicProvider } from './types'

export function getMyProviderProfile(): Promise<ProviderMe> {
  return apiRequest('/api/providers/me')
}

export function updateMyProviderProfile(payload: {
  city: string
  yearsOfExperience: number
  bio?: string | null
}): Promise<ProviderMe> {
  return apiRequest('/api/providers/me', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function replaceMyServices(serviceIds: number[]): Promise<ProviderMe> {
  return apiRequest('/api/providers/me/services', {
    method: 'PUT',
    body: JSON.stringify({ serviceIds }),
  })
}

export function getPublicProvider(id: number): Promise<PublicProvider> {
  return apiRequest(`/api/providers/${id}`)
}

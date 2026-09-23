import { apiRequest } from './client'
import type { ProviderMe, PublicProvider } from './types'

export function providerPhotoUrl(providerId: number, cacheKey?: string | number): string {
  const version = cacheKey == null ? '' : `?v=${encodeURIComponent(String(cacheKey))}`
  return `/api/providers/${providerId}/photo${version}`
}

export function getMyProviderProfile(): Promise<ProviderMe> {
  return apiRequest('/api/providers/me')
}

export function updateMyProviderProfile(payload: {
  city: string
  phoneNumber: string
  yearsOfExperience: number
  bio?: string | null
  latitude?: number | null
  longitude?: number | null
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

export function requestLocationChange(payload: {
  city: string
  latitude?: number | null
  longitude?: number | null
}): Promise<ProviderMe> {
  return apiRequest('/api/providers/me/location-changes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function requestServiceAddition(
  serviceId: number,
  documentType: string,
  file: File,
): Promise<ProviderMe> {
  const body = new FormData()
  body.set('serviceId', String(serviceId))
  body.set('documentType', documentType)
  body.set('file', file)
  return apiRequest('/api/providers/me/service-changes', {
    method: 'POST',
    body,
  })
}

export function uploadMyPhoto(file: File): Promise<ProviderMe> {
  const body = new FormData()
  body.set('file', file)
  return apiRequest('/api/providers/me/photo', {
    method: 'POST',
    body,
  })
}

export function deleteMyPhoto(): Promise<ProviderMe> {
  return apiRequest('/api/providers/me/photo', { method: 'DELETE' })
}

export function getPublicProvider(id: number): Promise<PublicProvider> {
  return apiRequest(`/api/providers/${id}`)
}

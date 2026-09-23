import { apiRequest } from './client'
import type { WorkingHour } from './types'

export type Category = {
  id: number
  name: string
  description: string
  hasImage: boolean
}

export type CatalogService = {
  id: number
  name: string
  description: string
  categoryId: number
  categoryName: string
  hasImage: boolean
}

export function categoryImageUrl(id: number) {
  return `/api/catalog/categories/${id}/image`
}

export function serviceImageUrl(id: number) {
  return `/api/catalog/services/${id}/image`
}

export function getCategories(): Promise<Category[]> {
  return apiRequest<Category[]>('/api/catalog/categories')
}

export type ServiceProvider = {
  id: number
  fullName: string
  city: string
  yearsOfExperience: number
  bio: string | null
  isVerified: boolean
  averageRating: number
  reviewCount: number
  hasPhoto: boolean
  completedJobs: number
  workingHours: WorkingHour[]
}

export function getServices(): Promise<CatalogService[]> {
  return apiRequest<CatalogService[]>('/api/catalog/services')
}

export function getService(id: number): Promise<CatalogService> {
  return apiRequest<CatalogService>(`/api/catalog/services/${id}`)
}

export function getServiceProviders(
  serviceId: number,
  city?: string,
): Promise<ServiceProvider[]> {
  const query = city ? `?city=${encodeURIComponent(city)}` : ''
  return apiRequest<ServiceProvider[]>(
    `/api/catalog/services/${serviceId}/providers${query}`,
  )
}

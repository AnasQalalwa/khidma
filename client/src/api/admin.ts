import { apiRequest } from './client'
import { toQuery } from './query'
import type { Category, CatalogService } from './catalog'
import type { AdminProvider, AdminStats, PageQuery, PagedResult } from './types'

export function getAdminStats(): Promise<AdminStats> {
  return apiRequest('/api/admin/stats')
}

export function getAdminProviders(
  query: PageQuery & { approved?: boolean } = {},
): Promise<PagedResult<AdminProvider>> {
  return apiRequest(`/api/admin/providers${toQuery(query)}`)
}

export function setProviderApproval(
  id: number,
  isApproved: boolean,
): Promise<AdminProvider> {
  return apiRequest(`/api/admin/providers/${id}/approval`, {
    method: 'POST',
    body: JSON.stringify({ isApproved }),
  })
}

export function createCategory(name: string): Promise<Category> {
  return apiRequest('/api/admin/categories', {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

export function updateCategory(id: number, name: string): Promise<Category> {
  return apiRequest(`/api/admin/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name }),
  })
}

export function deleteCategory(id: number): Promise<boolean> {
  return apiRequest(`/api/admin/categories/${id}`, { method: 'DELETE' })
}

export function createService(payload: {
  name: string
  categoryId: number
}): Promise<CatalogService> {
  return apiRequest('/api/admin/services', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateService(
  id: number,
  payload: { name: string; categoryId: number },
): Promise<CatalogService> {
  return apiRequest(`/api/admin/services/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteService(id: number): Promise<boolean> {
  return apiRequest(`/api/admin/services/${id}`, { method: 'DELETE' })
}

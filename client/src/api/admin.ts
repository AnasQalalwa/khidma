import { apiDownload, apiRequest } from './client'
import { toQuery } from './query'
import type {
  AdminProvider,
  AdminStats,
  AdminAttention,
  AdminOverview,
  AdminUser,
  AdminUserDetail,
  PageQuery,
  PagedResult,
} from './types'
import type { Category, CatalogService } from './catalog'

export function getAdminStats(): Promise<AdminStats> {
  return apiRequest('/api/admin/stats')
}

export function getAdminOverview(range: 'today' | '7d' | '30d'): Promise<AdminOverview> {
  return apiRequest(`/api/admin/stats/overview${toQuery({ range })}`)
}

export function getAdminAttention(): Promise<AdminAttention> {
  return apiRequest('/api/admin/attention')
}

export function getAdminProviders(
  query: PageQuery & {
    verificationStatus?: string
    suspended?: boolean
    search?: string
  } = {},
): Promise<PagedResult<AdminProvider>> {
  return apiRequest(`/api/admin/providers${toQuery(query)}`)
}

export function getAdminUsers(
  query: PageQuery & {
    search?: string
    role?: string
    providerVerificationStatus?: string
    suspended?: boolean
  } = {},
): Promise<PagedResult<AdminUser>> {
  return apiRequest(`/api/admin/users${toQuery(query)}`)
}

export function getAdminUser(userId: string): Promise<AdminUserDetail> {
  return apiRequest(`/api/admin/users/${userId}`)
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

export function downloadAdminDocument(documentId: number, fileName: string): Promise<void> {
  return apiDownload(`/api/admin/verification-documents/${documentId}/download`, fileName)
}

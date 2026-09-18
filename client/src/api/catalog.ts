import { apiRequest } from './client'

export type Category = {
  id: number
  name: string
}

export type CatalogService = {
  id: number
  name: string
  categoryId: number
  categoryName: string
}

export function getCategories(): Promise<Category[]> {
  return apiRequest<Category[]>('/api/catalog/categories')
}

export function getServices(): Promise<CatalogService[]> {
  return apiRequest<CatalogService[]>('/api/catalog/services')
}

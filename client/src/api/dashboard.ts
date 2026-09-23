import { apiRequest } from './client'
import type { ProviderDashboard } from './types'

export function getProviderDashboard(): Promise<ProviderDashboard> {
  return apiRequest('/api/dashboard/provider')
}

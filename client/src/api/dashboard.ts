import { apiRequest } from './client'
import type { CustomerDashboard, ProviderDashboard } from './types'

export function getCustomerDashboard(): Promise<CustomerDashboard> {
  return apiRequest('/api/dashboard/customer')
}

export function getProviderDashboard(): Promise<ProviderDashboard> {
  return apiRequest('/api/dashboard/provider')
}

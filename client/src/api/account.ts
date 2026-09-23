import { apiRequest } from './client'
import type { AccountProfile, UpdateAccountProfilePayload } from './types'

export function getAccountProfile(): Promise<AccountProfile> {
  return apiRequest('/api/account/profile')
}

export function updateAccountProfile(
  payload: UpdateAccountProfilePayload,
): Promise<AccountProfile> {
  return apiRequest('/api/account/profile', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function changePassword(payload: {
  currentPassword: string
  newPassword: string
}): Promise<void> {
  return apiRequest('/api/account/password', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

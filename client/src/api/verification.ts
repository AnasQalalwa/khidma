import { apiDownload, apiRequest } from './client'
import { toQuery } from './query'
import type {
  AdminVerificationListItem,
  PageQuery,
  PagedResult,
  ProviderVerification,
  VerificationDocument,
  VerificationDocumentType,
} from './types'

export function getMyVerification(): Promise<ProviderVerification> {
  return apiRequest('/api/providers/me/verification')
}

export function uploadVerificationDocument(
  documentType: VerificationDocumentType,
  file: File,
): Promise<ProviderVerification> {
  const body = new FormData()
  body.set('documentType', documentType)
  body.set('file', file)
  return apiRequest('/api/providers/me/verification-documents', {
    method: 'POST',
    body,
  })
}

export function deleteVerificationDocument(documentId: number): Promise<ProviderVerification> {
  return apiRequest(`/api/providers/me/verification-documents/${documentId}`, {
    method: 'DELETE',
  })
}

export function downloadMyVerificationDocument(
  documentId: number,
  fileName: string,
): Promise<void> {
  return apiDownload(
    `/api/providers/me/verification-documents/${documentId}/download`,
    fileName,
  )
}

export function getAdminVerifications(
  query: PageQuery & {
    verificationStatus?: string
    documentStatus?: string
    search?: string
  } = {},
): Promise<PagedResult<AdminVerificationListItem>> {
  return apiRequest(`/api/admin/verifications${toQuery(query)}`)
}

export function getAdminVerification(providerId: number): Promise<ProviderVerification> {
  return apiRequest(`/api/admin/providers/${providerId}/verification`)
}

export function reviewVerificationDocument(
  documentId: number,
  payload: { status: 'Approved' | 'Rejected'; note?: string },
): Promise<VerificationDocument> {
  return apiRequest(`/api/admin/verification-documents/${documentId}/review`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function decideProviderVerification(
  providerId: number,
  payload: { status: 'Approved' | 'Rejected' | 'PendingReview'; reason?: string },
): Promise<ProviderVerification> {
  return apiRequest(`/api/admin/providers/${providerId}/verification`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function setProviderSuspension(
  providerId: number,
  payload: { suspended: boolean; reason?: string },
): Promise<ProviderVerification> {
  return apiRequest(`/api/admin/providers/${providerId}/suspension`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

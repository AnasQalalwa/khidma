import { apiRequest } from './client'
import { toQuery } from './query'
import type {
  AuditFilterOptions,
  AuditLogDetail,
  AuditLogItem,
  AuditSummary,
  PageQuery,
  PagedResult,
} from './types'

export function getAuditLogs(
  query: PageQuery & {
    from?: string
    to?: string
    actorUserId?: string
    actorEmail?: string
    category?: string
    action?: string
    entityType?: string
    entityId?: string
    outcome?: string
    search?: string
    hideAuth?: boolean
  } = {},
): Promise<PagedResult<AuditLogItem>> {
  return apiRequest(`/api/admin/audit-logs${toQuery(query)}`)
}

export function getAuditLog(id: number): Promise<AuditLogDetail> {
  return apiRequest(`/api/admin/audit-logs/${id}`)
}

export function getAuditSummary(): Promise<AuditSummary> {
  return apiRequest('/api/admin/audit-logs/summary')
}

export function getAuditFilterOptions(): Promise<AuditFilterOptions> {
  return apiRequest('/api/admin/audit-logs/options')
}

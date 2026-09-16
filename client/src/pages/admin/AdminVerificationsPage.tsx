import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getAdminVerifications } from '../../api/verification'
import { ApiError } from '../../api/client'
import type { AdminVerificationListItem, PagedResult, ProviderVerificationStatus } from '../../api/types'
import { Roles } from '../../auth/roles'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'

const TABS: { value: ProviderVerificationStatus | ''; label: string }[] = [
  { value: 'PendingReview', label: 'Pending Review' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: '', label: 'All' },
]

export function AdminVerificationsPage() {
  const [params, setParams] = useSearchParams()
  const tab = (params.get('verificationStatus') as ProviderVerificationStatus | '') ?? 'PendingReview'
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<AdminVerificationListItem> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getAdminVerifications({
          page,
          pageSize: 12,
          verificationStatus: tab || undefined,
          documentStatus: params.get('documentStatus') || undefined,
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load verifications.')
    } finally {
      setLoading(false)
    }
  }, [page, params, tab])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <PageHeader
        eyebrow="Admin"
        title="Provider verification"
        description="Review professional proof documents before a provider can receive new work."
      />
      <div className="tabs" role="tablist" aria-label="Verification status">
        {TABS.map((item) => (
          <button
            key={item.label}
            type="button"
            role="tab"
            className={item.value === tab ? 'tab-btn is-active' : 'tab-btn'}
            aria-selected={item.value === tab}
            onClick={() => {
              setPage(1)
              const next = new URLSearchParams(params)
              next.set('verificationStatus', item.value)
              setParams(next)
            }}
          >
            {item.label}
          </button>
        ))}
      </div>
      {loading ? <LoadingState label="Loading verifications" /> : null}
      {error ? (
        <ErrorState
          title="Unable to load verifications"
          description={error}
          onRetry={() => void load()}
        />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState
          title="No providers in this queue"
          description="Providers appear here after they register and upload professional proof."
        />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="card-grid">
            {data.items.map((provider) => (
              <article className="request-card" key={provider.providerProfileId}>
                <div className="request-card-head">
                  <div>
                    <h3>{provider.fullName}</h3>
                    <p className="muted">{provider.email}</p>
                  </div>
                  <StatusBadge
                    status={provider.isSuspended ? 'Suspended' : provider.verificationStatus}
                  />
                </div>
                <p>
                  {provider.city} · {provider.yearsOfExperience} years
                </p>
                <p className="muted">{provider.services.join(', ') || 'No services yet'}</p>
                <p>
                  Documents {provider.documentCount} · Pending {provider.pendingDocumentCount} ·
                  Approved {provider.approvedDocumentCount} · Rejected {provider.rejectedDocumentCount}
                </p>
                <Link className="btn btn-secondary btn-sm" to={`/admin/verifications/${provider.providerProfileId}`}>
                  Open review
                </Link>
              </article>
            ))}
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

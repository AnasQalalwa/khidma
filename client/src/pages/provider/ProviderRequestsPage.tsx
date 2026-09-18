import { useCallback, useEffect, useState } from 'react'
import { getAvailableRequests } from '../../api/requests'
import { ApiError } from '../../api/client'
import type { PagedResult, RequestSummaryForProvider } from '../../api/types'
import { Roles } from '../../auth/roles'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { RequestCard } from '../../components/RequestCard'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'

export function ProviderRequestsPage() {
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<RequestSummaryForProvider> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await getAvailableRequests({ page, pageSize: 12 }))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load requests.')
    } finally {
      setLoading(false)
    }
  }, [page])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Provider}>
      <PageHeader
        eyebrow="Provider"
        title="Available requests"
        description="Only open requests that match your city and services are shown."
      />
      {loading ? <LoadingState label="Loading available requests" /> : null}
      {error ? (
        <ErrorState title="Unable to load requests" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState
          title="No matching requests"
          description="Complete your profile, wait for admin approval, and keep your city and services up to date."
        />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="card-grid">
            {data.items.map((request) => (
              <RequestCard
                key={request.id}
                request={request}
                href={`/provider/requests/${request.id}`}
              />
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

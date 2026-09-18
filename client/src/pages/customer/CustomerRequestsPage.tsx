import { useCallback, useEffect, useState } from 'react'
import { Plus } from 'lucide-react'
import { getMyRequests } from '../../api/requests'
import { ApiError } from '../../api/client'
import type { PagedResult, ServiceRequestSummary } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { RequestCard } from '../../components/RequestCard'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'

export function CustomerRequestsPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<ServiceRequestSummary> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await getMyRequests({
        page,
        pageSize: 12,
        status: status || undefined,
      })
      setData(result)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load requests.')
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Customer}>
      <PageHeader
        eyebrow="Customer"
        title="My requests"
        description="Track open jobs, incoming offers, and completed work."
        actions={
          <Button to="/customer/requests/new" icon={Plus}>
            Create a request
          </Button>
        }
      />
      <div className="filter-bar">
        <label htmlFor="request-status">
          Status
          <select
            id="request-status"
            value={status}
            onChange={(event) => {
              setPage(1)
              setStatus(event.target.value)
            }}
          >
            <option value="">All</option>
            <option value="Open">Open</option>
            <option value="Booked">Booked</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </label>
      </div>
      {loading ? <LoadingState label="Loading your requests" /> : null}
      {error ? (
        <ErrorState title="Unable to load requests" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState
          title="No service requests yet."
          description="Create a request to start receiving offers from local providers."
          action={
            <Button to="/customer/requests/new" icon={Plus}>
              Create a request
            </Button>
          }
        />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="card-grid">
            {data.items.map((request) => (
              <RequestCard
                key={request.id}
                request={request}
                href={`/customer/requests/${request.id}`}
                showOffers
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

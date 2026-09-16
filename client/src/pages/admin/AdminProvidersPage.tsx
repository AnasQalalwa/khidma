import { useCallback, useEffect, useState } from 'react'
import {
  getAdminProviders,
  setProviderApproval,
} from '../../api/admin'
import { ApiError } from '../../api/client'
import type { AdminProvider, PagedResult } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatRating } from '../../utils/format'

export function AdminProvidersPage() {
  const [page, setPage] = useState(1)
  const [approved, setApproved] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [data, setData] = useState<PagedResult<AdminProvider> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getAdminProviders({
          page,
          pageSize: 12,
          approved: approved === '' ? undefined : approved === 'true',
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load providers.')
    } finally {
      setLoading(false)
    }
  }, [approved, page])

  useEffect(() => {
    void load()
  }, [load])

  async function toggle(provider: AdminProvider) {
    setBusyId(provider.id)
    setActionError(null)
    try {
      await setProviderApproval(provider.id, !provider.isApproved)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not update approval.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <PageHeader
        eyebrow="Admin"
        title="Providers"
        description="Approve providers before they can see matching requests."
      />
      {actionError ? (
        <div className="alert" role="alert">
          {actionError}
        </div>
      ) : null}
      <div className="filter-bar">
        <label htmlFor="provider-approval">
          Approval
          <select
            id="provider-approval"
            value={approved}
            onChange={(event) => {
              setPage(1)
              setApproved(event.target.value)
            }}
          >
            <option value="">All</option>
            <option value="true">Approved</option>
            <option value="false">Pending</option>
          </select>
        </label>
      </div>
      {loading ? <LoadingState label="Loading providers" /> : null}
      {error ? (
        <ErrorState title="Unable to load providers" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState title="No providers" description="Registered providers will appear here." />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Provider</th>
                  <th>City</th>
                  <th>Services</th>
                  <th>Rating</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((provider) => (
                  <tr key={provider.id}>
                    <td>
                      <strong>{provider.fullName}</strong>
                      <div className="muted">{provider.email}</div>
                    </td>
                    <td>{provider.city}</td>
                    <td>{provider.services.join(', ') || '—'}</td>
                    <td>{formatRating(provider.averageRating, provider.reviewCount)}</td>
                    <td>
                      <StatusBadge status={provider.isApproved ? 'Approved' : 'Pending'} />
                    </td>
                    <td>
                      <Button
                        size="sm"
                        variant={provider.isApproved ? 'secondary' : 'primary'}
                        loading={busyId === provider.id}
                        onClick={() => void toggle(provider)}
                      >
                        {provider.isApproved ? 'Unapprove' : 'Approve'}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-grid admin-card-grid">
            {data.items.map((provider) => (
              <article className="request-card" key={`card-${provider.id}`}>
                <div className="request-card-head">
                  <div>
                    <h3>{provider.fullName}</h3>
                    <p className="muted">{provider.email}</p>
                  </div>
                  <StatusBadge status={provider.isApproved ? 'Approved' : 'Pending'} />
                </div>
                <p>{provider.city}</p>
                <p className="muted">{provider.services.join(', ') || 'No services'}</p>
                <p className="muted">
                  {formatRating(provider.averageRating, provider.reviewCount)}
                </p>
                <Button
                  size="sm"
                  loading={busyId === provider.id}
                  onClick={() => void toggle(provider)}
                >
                  {provider.isApproved ? 'Unapprove' : 'Approve'}
                </Button>
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

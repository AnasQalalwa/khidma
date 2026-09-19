import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getAdminProviders } from '../../api/admin'
import { ApiError } from '../../api/client'
import { setProviderSuspension } from '../../api/verification'
import type { AdminProvider, PagedResult } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { AdminPageHeader } from '../../components/admin/AdminPageHeader'
import { Pagination } from '../../components/Pagination'
import { ReasonDialog } from '../../components/ReasonDialog'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatRating } from '../../utils/format'

const FILTERS = [
  { value: 'active', label: 'Active' },
  { value: 'suspended', label: 'Suspended' },
  { value: 'pending', label: 'Pending' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'all', label: 'All' },
] as const

function queryForFilter(filter: string) {
  if (filter === 'active') {
    return { verificationStatus: 'Approved', suspended: false as const }
  }
  if (filter === 'suspended') {
    return { suspended: true as const }
  }
  if (filter === 'pending') {
    return { verificationStatus: 'PendingReview' }
  }
  if (filter === 'rejected') {
    return { verificationStatus: 'Rejected' }
  }
  return {}
}

export function AdminProvidersPage() {
  const [params, setParams] = useSearchParams()
  const filter =
    params.get('suspended') === 'true'
      ? 'suspended'
      : (params.get('filter') ?? 'all')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState(params.get('search') ?? '')
  const [submittedSearch, setSubmittedSearch] = useState(params.get('search') ?? '')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<AdminProvider> | null>(null)
  const [suspendTarget, setSuspendTarget] = useState<AdminProvider | null>(null)
  const [reactivateTarget, setReactivateTarget] = useState<AdminProvider | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getAdminProviders({
          page,
          pageSize: 12,
          search: submittedSearch || undefined,
          ...queryForFilter(filter),
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load providers.')
    } finally {
      setLoading(false)
    }
  }, [filter, page, submittedSearch])

  useEffect(() => {
    void load()
  }, [load])

  function setFilter(next: string) {
    setPage(1)
    const updated = new URLSearchParams(params)
    if (next === 'all') {
      updated.delete('filter')
      updated.delete('suspended')
    } else if (next === 'suspended') {
      updated.set('suspended', 'true')
      updated.delete('filter')
    } else {
      updated.set('filter', next)
      updated.delete('suspended')
    }
    setParams(updated)
  }

  async function suspend(reason: string) {
    if (!suspendTarget) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      await setProviderSuspension(suspendTarget.id, { suspended: true, reason })
      setSuspendTarget(null)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not suspend this provider.')
    } finally {
      setBusy(false)
    }
  }

  async function reactivate() {
    if (!reactivateTarget) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      await setProviderSuspension(reactivateTarget.id, { suspended: false })
      setReactivateTarget(null)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not reactivate this provider.')
    } finally {
      setBusy(false)
    }
  }

  function providerStatus(provider: AdminProvider) {
    if (provider.isSuspended) {
      return 'Suspended'
    }
    return provider.verificationStatus
  }

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <AdminPageHeader
        title="Providers"
        subtitle="Operational control: suspend access to new work, or reactivate a provider who is already verified."
      />
      <p className="muted">
        Suspension rejects pending offers and hides matching requests. Existing bookings stay
        in place so in-progress jobs can still be completed.
      </p>
      {actionError ? (
        <div className="alert" role="alert">
          {actionError}
        </div>
      ) : null}
      <form
        className="filter-bar"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          setSubmittedSearch(search.trim())
        }}
      >
        <label htmlFor="provider-filter">
          Status
          <select
            id="provider-filter"
            value={filter}
            onChange={(event) => setFilter(event.target.value)}
          >
            {FILTERS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="filter-search" htmlFor="provider-search">
          Search name or email
          <input
            id="provider-search"
            type="search"
            placeholder="Name or email"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </label>
        <button className="btn btn-secondary btn-sm" type="submit">
          Search
        </button>
      </form>
      {loading ? <LoadingState label="Loading providers" /> : null}
      {error ? (
        <ErrorState title="Unable to load providers" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState title="No providers" description="No providers match the current filters." />
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
                      <StatusBadge status={providerStatus(provider)} />
                    </td>
                    <td>
                      <div className="inline-actions">
                        <Link to={`/admin/verifications/${provider.id}`}>Verification</Link>
                        {provider.isSuspended ? (
                          <Button size="sm" onClick={() => setReactivateTarget(provider)}>
                            Reactivate
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setSuspendTarget(provider)}
                          >
                            Suspend
                          </Button>
                        )}
                      </div>
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
                  <StatusBadge status={providerStatus(provider)} />
                </div>
                <p>{provider.city}</p>
                <p className="muted">{provider.services.join(', ') || 'No services'}</p>
                {provider.isSuspended && provider.suspensionReason ? (
                  <p>Reason: {provider.suspensionReason}</p>
                ) : null}
                {provider.isSuspended ? (
                  <Button size="sm" onClick={() => setReactivateTarget(provider)}>
                    Reactivate
                  </Button>
                ) : (
                  <Button size="sm" variant="secondary" onClick={() => setSuspendTarget(provider)}>
                    Suspend
                  </Button>
                )}
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

      <ReasonDialog
        open={suspendTarget !== null}
        title="Suspend this provider?"
        description="They will stop receiving new work. Pending offers are rejected. Existing bookings continue."
        confirmLabel="Suspend provider"
        label="Suspension reason"
        danger
        busy={busy}
        onConfirm={(reason) => void suspend(reason)}
        onClose={() => setSuspendTarget(null)}
      />
      <ConfirmDialog
        open={reactivateTarget !== null}
        title="Reactivate this provider?"
        description="Suspension fields are cleared. The provider can receive new work only if they are already approved."
        confirmLabel="Reactivate"
        busy={busy}
        onConfirm={() => void reactivate()}
        onClose={() => setReactivateTarget(null)}
      />
    </WorkspaceLayout>
  )
}

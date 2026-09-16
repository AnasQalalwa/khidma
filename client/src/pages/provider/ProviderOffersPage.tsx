import { useCallback, useEffect, useState } from 'react'
import { withdrawOffer, getMyOffers } from '../../api/offers'
import { ApiError } from '../../api/client'
import type { OfferMine, PagedResult } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate, formatMoney } from '../../utils/format'

export function ProviderOffersPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [data, setData] = useState<PagedResult<OfferMine> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getMyOffers({
          page,
          pageSize: 12,
          status: status || undefined,
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load offers.')
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  async function handleWithdraw(id: number) {
    setBusyId(id)
    setActionError(null)
    try {
      await withdrawOffer(id)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not withdraw the offer.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Provider}>
      <PageHeader
        eyebrow="Provider"
        title="My offers"
        description="Track pending, accepted, rejected, and withdrawn offers."
      />
      {actionError ? (
        <div className="alert" role="alert">
          {actionError}
        </div>
      ) : null}
      <div className="filter-bar">
        <label htmlFor="offer-status">
          Status
          <select
            id="offer-status"
            value={status}
            onChange={(event) => {
              setPage(1)
              setStatus(event.target.value)
            }}
          >
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Accepted">Accepted</option>
            <option value="Rejected">Rejected</option>
            <option value="Withdrawn">Withdrawn</option>
          </select>
        </label>
      </div>
      {loading ? <LoadingState label="Loading offers" /> : null}
      {error ? (
        <ErrorState title="Unable to load offers" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState
          title="No offers yet"
          description="Open an eligible request to submit your first offer."
        />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="card-grid">
            {data.items.map((offer) => (
              <article className="offer-card" key={offer.id}>
                <div className="offer-card-head">
                  <div>
                    <p className="meta">
                      {offer.categoryName} · {offer.serviceName}
                    </p>
                    <h3>{offer.requestTitle}</h3>
                  </div>
                  <StatusBadge status={offer.status} />
                </div>
                <p className="offer-price">{formatMoney(offer.price)}</p>
                <p>{offer.message}</p>
                <p className="meta">
                  {offer.city} · Estimated {formatDate(offer.estimatedDate)}
                </p>
                <div className="dashboard-actions">
                  <Button to={`/provider/requests/${offer.serviceRequestId}`} size="sm" variant="secondary">
                    View request
                  </Button>
                  {offer.status === 'Pending' ? (
                    <Button
                      size="sm"
                      variant="ghost"
                      loading={busyId === offer.id}
                      onClick={() => void handleWithdraw(offer.id)}
                    >
                      Withdraw
                    </Button>
                  ) : null}
                </div>
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

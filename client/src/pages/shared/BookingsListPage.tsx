import { useCallback, useEffect, useState } from 'react'
import { getMyBookings } from '../../api/bookings'
import { ApiError } from '../../api/client'
import type { BookingSummary, PagedResult } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDay, formatMoney, formatSlot } from '../../utils/format'

const CUSTOMER_FILTERS = [
  { id: 'pending', label: 'Pending', status: 'Pending' },
  { id: 'upcoming', label: 'Upcoming', status: 'Scheduled,InProgress' },
  { id: 'past', label: 'Past', status: 'Completed,Declined,Cancelled' },
] as const

export function BookingsListPage({ role }: { role: 'Customer' | 'Provider' }) {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState(role === 'Customer' ? CUSTOMER_FILTERS[0].status : '')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<BookingSummary> | null>(null)
  const base = role === 'Customer' ? '/account' : '/provider'

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getMyBookings({
          page,
          pageSize: 12,
          status: status || undefined,
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load bookings.')
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={role === 'Customer' ? Roles.Customer : Roles.Provider}>
      <PageHeader
        eyebrow={role}
        title={role === 'Customer' ? 'My bookings' : 'Bookings'}
        description={
          role === 'Customer'
            ? 'Pending requests, upcoming visits, and past jobs.'
            : 'Requests waiting for your price, plus jobs already on the calendar.'
        }
      />
      <div className="filter-bar">
        {role === 'Customer' ? (
          <div className="catalog-pills" role="toolbar" aria-label="Filter bookings">
            {CUSTOMER_FILTERS.map((filter) => (
              <button
                key={filter.id}
                type="button"
                className="catalog-pill"
                aria-pressed={status === filter.status}
                onClick={() => {
                  setPage(1)
                  setStatus(filter.status)
                }}
              >
                {filter.label}
              </button>
            ))}
          </div>
        ) : (
          <label htmlFor="booking-status">
            Status
            <select
              id="booking-status"
              value={status}
              onChange={(event) => {
                setPage(1)
                setStatus(event.target.value)
              }}
            >
              <option value="">All</option>
              <option value="Pending">Pending</option>
              <option value="Scheduled">Scheduled</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
              <option value="Declined">Declined</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </label>
        )}
      </div>
      {loading ? <LoadingState label="Loading bookings" /> : null}
      {error ? (
        <ErrorState title="Unable to load bookings" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState
          title="No bookings in this view"
          description={
            role === 'Customer'
              ? 'Book a provider from the catalog and the request will show up here.'
              : 'New booking requests from customers will appear here.'
          }
        />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="card-grid">
            {data.items.map((booking) => (
              <article className="request-card" key={booking.id}>
                <div className="request-card-head">
                  <div>
                    <p className="meta">
                      {booking.categoryName} · {booking.city}
                    </p>
                    <h3>{booking.serviceName}</h3>
                  </div>
                  <StatusBadge status={booking.status} />
                </div>
                <p className="muted">{booking.counterpartyName}</p>
                <dl className="request-meta">
                  <div>
                    <dt>{booking.scheduledStart ? 'Scheduled' : 'Requested'}</dt>
                    <dd>
                      {booking.scheduledStart
                        ? formatSlot(booking.scheduledStart, booking.scheduledEnd)
                        : formatDay(booking.requestedDate)}
                    </dd>
                  </div>
                  <div>
                    <dt>Price</dt>
                    <dd>{formatMoney(booking.quotedPrice)}</dd>
                  </div>
                </dl>
                <Button to={`${base}/bookings/${booking.id}`} variant="secondary" size="sm">
                  View
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

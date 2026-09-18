import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  cancelBooking,
  completeBooking,
  getBooking,
  startBooking,
} from '../../api/bookings'
import { ApiError } from '../../api/client'
import type { BookingDetail } from '../../api/types'
import { Roles } from '../../auth/roles'
import { BookingTimeline } from '../../components/BookingTimeline'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { ReviewForm } from '../../components/ReviewForm'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate, formatMoney } from '../../utils/format'

export function BookingDetailPage({ role }: { role: 'Customer' | 'Provider' }) {
  const { id } = useParams()
  const bookingId = Number(id)
  const [data, setData] = useState<BookingDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const [reason, setReason] = useState('')
  const requestHref =
    role === 'Customer'
      ? `/customer/requests/${data?.serviceRequestId ?? ''}`
      : `/provider/requests/${data?.serviceRequestId ?? ''}`

  const load = useCallback(async () => {
    if (Number.isNaN(bookingId)) {
      setError('Booking not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getBooking(bookingId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this booking.')
    } finally {
      setLoading(false)
    }
  }, [bookingId])

  useEffect(() => {
    void load()
  }, [load])

  async function run(action: () => Promise<BookingDetail>) {
    setBusy(true)
    setActionError(null)
    try {
      setData(await action())
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'The action could not be completed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <WorkspaceLayout role={role === 'Customer' ? Roles.Customer : Roles.Provider}>
      {loading ? <LoadingState label="Loading booking" count={2} /> : null}
      {error ? (
        <ErrorState title="Unable to load booking" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data ? (
        <>
          <PageHeader
            eyebrow={`${data.categoryName} · ${data.serviceName}`}
            title={data.title}
            description={data.description}
            actions={<StatusBadge status={data.status} />}
          />
          {actionError ? (
            <div className="alert" role="alert">
              {actionError}
            </div>
          ) : null}
          <BookingTimeline status={data.status} />
          <dl className="detail-grid">
            <div>
              <dt>Scheduled</dt>
              <dd>{formatDate(data.scheduledDate)}</dd>
            </div>
            <div>
              <dt>Final price</dt>
              <dd>{formatMoney(data.finalPrice)}</dd>
            </div>
            <div>
              <dt>City</dt>
              <dd>{data.city}</dd>
            </div>
            <div>
              <dt>Request</dt>
              <dd>
                <Link to={requestHref}>View request</Link>
              </dd>
            </div>
            <div>
              <dt>Customer</dt>
              <dd>
                {data.customerName}
                {data.customerEmail ? ` · ${data.customerEmail}` : ''}
              </dd>
            </div>
            <div>
              <dt>Provider</dt>
              <dd>
                <Link to={`/providers/${data.providerProfileId}`}>{data.providerName}</Link>
                {data.providerEmail ? ` · ${data.providerEmail}` : ''}
              </dd>
            </div>
            {data.customerContact ? (
              <div>
                <dt>Customer contact</dt>
                <dd>{data.customerContact}</dd>
              </div>
            ) : null}
            {data.startedAt ? (
              <div>
                <dt>Started</dt>
                <dd>{formatDate(data.startedAt)}</dd>
              </div>
            ) : null}
            {data.completedAt ? (
              <div>
                <dt>Completed</dt>
                <dd>{formatDate(data.completedAt)}</dd>
              </div>
            ) : null}
            {data.cancellationReason ? (
              <div>
                <dt>Cancellation reason</dt>
                <dd>{data.cancellationReason}</dd>
              </div>
            ) : null}
          </dl>
          <div className="dashboard-actions">
            {data.canStart ? (
              <Button loading={busy} onClick={() => void run(() => startBooking(data.id))}>
                Start work
              </Button>
            ) : null}
            {data.canComplete ? (
              <Button loading={busy} onClick={() => void run(() => completeBooking(data.id))}>
                Complete job
              </Button>
            ) : null}
            {data.canCancel ? (
              <Button variant="ghost" onClick={() => setCancelOpen(true)}>
                Cancel booking
              </Button>
            ) : null}
          </div>
          {data.review ? (
            <section className="dashboard-panel">
              <h2>Review</h2>
              <p>
                {data.review.rating} / 5 · {data.review.customerDisplayName}
              </p>
              {data.review.comment ? <p>{data.review.comment}</p> : null}
            </section>
          ) : null}
          {data.canReview ? (
            <section className="dashboard-panel">
              <h2>Leave a review</h2>
              <ReviewForm bookingId={data.id} onSubmitted={() => void load()} />
            </section>
          ) : null}
          <ConfirmDialog
            open={cancelOpen}
            title="Cancel this booking?"
            description="The related service request will also be cancelled. This is only allowed before work starts."
            confirmLabel="Cancel booking"
            danger
            busy={busy}
            onClose={() => setCancelOpen(false)}
            onConfirm={() => {
              if (!reason.trim()) {
                setActionError('A cancellation reason is required.')
                return
              }

              setBusy(true)
              setActionError(null)
              void cancelBooking(data.id, reason.trim())
                .then((booking) => {
                  setData(booking)
                  setCancelOpen(false)
                })
                .catch((err) => {
                  setActionError(
                    err instanceof ApiError
                      ? err.message
                      : 'The action could not be completed.',
                  )
                })
                .finally(() => setBusy(false))
            }}
          >
            <FormField label="Reason">
              <textarea
                rows={3}
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                required
              />
            </FormField>
          </ConfirmDialog>
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

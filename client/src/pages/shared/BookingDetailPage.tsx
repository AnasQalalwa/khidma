import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  acceptBooking,
  cancelBooking,
  completeBooking,
  declineBooking,
  getBooking,
  rescheduleBooking,
  startBooking,
} from '../../api/bookings'
import { ApiError } from '../../api/client'
import { getMySchedule } from '../../api/schedule'
import type { BookingDetail, BusyInterval, WorkingHour } from '../../api/types'
import { Roles } from '../../auth/roles'
import { BookingTimeline } from '../../components/BookingTimeline'
import { Button } from '../../components/Button'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { ReasonDialog } from '../../components/ReasonDialog'
import { ReviewForm } from '../../components/ReviewForm'
import { ScheduleFields } from '../../components/schedule/ScheduleFields'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate, formatDay, formatMoney, formatSlot } from '../../utils/format'
import { addDays, localSlot, toDateInput } from '../../utils/hours'
import { displayPhone } from '../../utils/validation'

export function BookingDetailPage({ role }: { role: 'Customer' | 'Provider' }) {
  const { id } = useParams()
  const bookingId = Number(id)
  const [data, setData] = useState<BookingDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const [declineOpen, setDeclineOpen] = useState(false)
  const [price, setPrice] = useState('')
  const [message, setMessage] = useState('')
  const [scheduleDate, setScheduleDate] = useState(() => toDateInput(addDays(new Date(), 1)))
  const [startHour, setStartHour] = useState(9)
  const [durationHours, setDurationHours] = useState(2)
  const [rescheduleOpen, setRescheduleOpen] = useState(false)
  const [rescheduleNote, setRescheduleNote] = useState('')
  const [hours, setHours] = useState<WorkingHour[] | null>(null)
  const [occupied, setOccupied] = useState<BusyInterval[]>([])
  const listHref = role === 'Customer' ? '/account/bookings' : '/provider/bookings'

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

  useEffect(() => {
    if (!data || role !== 'Provider' || (!data.canAccept && !data.canReschedule)) {
      return
    }

    const from = toDateInput(new Date())
    const to = toDateInput(addDays(new Date(), 60))
    let cancelled = false
    void getMySchedule(from, to)
      .then((schedule) => {
        if (!cancelled) {
          setHours(schedule.workingHours)
          setOccupied(
            schedule.items
              .filter(
                (item) =>
                  item.bookingId !== data.id &&
                  item.start &&
                  item.end &&
                  item.status !== 'Pending',
              )
              .map((item) => ({ start: item.start as string, end: item.end as string })),
          )
        }
      })
      .catch(() => {
        if (!cancelled) {
          setHours(null)
        }
      })

    return () => {
      cancelled = true
    }
  }, [data, role])

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

  async function handleAccept(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!data) {
      return
    }

    const amount = Number(price)
    if (!Number.isFinite(amount) || amount < 0.01) {
      setActionError('Enter a price of at least 0.01.')
      return
    }

    const slot = localSlot(scheduleDate, startHour, durationHours)
    await run(() =>
      acceptBooking(data.id, {
        price: amount,
        message: message.trim() || undefined,
        scheduledStart: slot.start.toISOString(),
        durationHours,
      }),
    )
  }

  async function handleReschedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!data) {
      return
    }

    const slot = localSlot(scheduleDate, startHour, durationHours)
    await run(() =>
      rescheduleBooking(data.id, {
        scheduledStart: slot.start.toISOString(),
        durationHours,
        note: rescheduleNote.trim() || undefined,
      }),
    )
    setRescheduleOpen(false)
  }

  const counterpartyName = data
    ? role === 'Customer'
      ? data.providerName
      : data.customerName
    : ''
  const counterpartyPhone = data
    ? role === 'Customer'
      ? data.providerPhone
      : data.customerPhone
    : null

  return (
    <WorkspaceLayout role={role === 'Customer' ? Roles.Customer : Roles.Provider}>
      {loading ? <LoadingState label="Loading booking" count={2} /> : null}
      {error ? (
        <ErrorState title="Unable to load booking" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data ? (
        <div className="booking-layout">
          <div className="booking-main">
          <PageHeader
            eyebrow={`${data.categoryName} · ${data.city}`}
            title={data.serviceName}
            description={data.notes ?? 'No extra notes were added.'}
            actions={<StatusBadge status={data.status} />}
          />
          {actionError ? (
            <div className="alert" role="alert">
              {actionError}
            </div>
          ) : null}
          {data.rescheduledAt ? (
            <div className="alert" role="status">
              Rescheduled {formatDate(data.rescheduledAt)}
              {data.rescheduleNote ? `: ${data.rescheduleNote}` : '.'}
            </div>
          ) : null}
          <section className="dashboard-panel">
            <h2>Visit</h2>
            <p>
              {data.scheduledStart
                ? formatSlot(data.scheduledStart, data.scheduledEnd)
                : `Requested for ${formatDay(data.requestedDate)}`}
            </p>
            {data.durationHours ? <p className="muted">{data.durationHours} hours</p> : null}
          </section>
          <dl className="detail-grid">
            <div>
              <dt>Requested day</dt>
              <dd>{formatDay(data.requestedDate)}</dd>
            </div>
            <div>
              <dt>Quoted price</dt>
              <dd>{formatMoney(data.quotedPrice)}</dd>
            </div>
            <div>
              <dt>Customer</dt>
              <dd>{data.customerName}</dd>
            </div>
            <div>
              <dt>Provider</dt>
              <dd>
                <Link to={`/providers/${data.providerProfileId}`}>{data.providerName}</Link>
              </dd>
            </div>
            {data.providerMessage ? (
              <div>
                <dt>Provider message</dt>
                <dd>{data.providerMessage}</dd>
              </div>
            ) : null}
            {data.declineReason ? (
              <div>
                <dt>Decline reason</dt>
                <dd>{data.declineReason}</dd>
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
          {data.canAccept ? (
            <form className="form dashboard-panel" onSubmit={(event) => void handleAccept(event)}>
              <h2>Accept and quote a price</h2>
              <ScheduleFields
                date={scheduleDate}
                startHour={startHour}
                durationHours={durationHours}
                hours={hours}
                busy={occupied}
                onDateChange={setScheduleDate}
                onStartHourChange={setStartHour}
                onDurationChange={setDurationHours}
              />
              <FormField label="Price" hint="Quote the amount you agreed on the call.">
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={price}
                  onChange={(event) => setPrice(event.target.value)}
                  required
                />
              </FormField>
              <FormField label="Message">
                <textarea
                  rows={3}
                  value={message}
                  onChange={(event) => setMessage(event.target.value)}
                  maxLength={1000}
                />
              </FormField>
              <Button type="submit" loading={busy}>
                Accept booking
              </Button>
            </form>
          ) : null}
          <div className="dashboard-actions">
            {data.canDecline ? (
              <Button variant="ghost" onClick={() => setDeclineOpen(true)}>
                Decline
              </Button>
            ) : null}
            {data.canReschedule ? (
              <Button variant="secondary" onClick={() => setRescheduleOpen(true)}>
                Reschedule
              </Button>
            ) : null}
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
            <Button to={listHref} variant="secondary">
              Back to bookings
            </Button>
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
          </div>
          <aside className="booking-aside dashboard-panel">
            <h2>Contact</h2>
            <p>
              {role === 'Customer' ? (
                <Link to={`/providers/${data.providerProfileId}`}>{counterpartyName}</Link>
              ) : (
                counterpartyName
              )}
            </p>
            {counterpartyPhone ? (
              <p>
                <a href={`tel:${displayPhone(counterpartyPhone).replace(/\s/g, '')}`}>
                  {displayPhone(counterpartyPhone)}
                </a>
              </p>
            ) : (
              <p className="muted">Phone number is not available.</p>
            )}
            <p className="muted">
              Call to confirm the visit. Phone numbers stay on this booking and are not shown on public profiles.
            </p>
            <BookingTimeline status={data.status} />
            <p className="booking-price">{formatMoney(data.quotedPrice)}</p>
          </aside>
          {rescheduleOpen ? (
            <div className="dialog-backdrop" onClick={() => (busy ? undefined : setRescheduleOpen(false))}>
              <form
                className="dialog-panel"
                role="dialog"
                aria-modal="true"
                aria-labelledby="reschedule-title"
                onClick={(event) => event.stopPropagation()}
                onSubmit={(event) => void handleReschedule(event)}
              >
                <h2 id="reschedule-title">Change the visit</h2>
                <p className="muted">Move the job or change how long it takes. The customer will see the update.</p>
                <ScheduleFields
                  date={scheduleDate}
                  startHour={startHour}
                  durationHours={durationHours}
                  hours={hours}
                  busy={occupied}
                  onDateChange={setScheduleDate}
                  onStartHourChange={setStartHour}
                  onDurationChange={setDurationHours}
                />
                <FormField label="Note">
                  <textarea
                    rows={3}
                    value={rescheduleNote}
                    onChange={(event) => setRescheduleNote(event.target.value)}
                    maxLength={500}
                  />
                </FormField>
                <div className="dialog-actions">
                  <Button variant="secondary" onClick={() => setRescheduleOpen(false)} disabled={busy}>
                    Cancel
                  </Button>
                  <Button type="submit" loading={busy}>
                    Save schedule
                  </Button>
                </div>
              </form>
            </div>
          ) : null}
          <ReasonDialog
            open={declineOpen}
            title="Decline this request?"
            description="The customer will see your reason. Declined requests cannot be accepted later."
            confirmLabel="Decline request"
            label="Reason"
            danger
            busy={busy}
            onClose={() => setDeclineOpen(false)}
            onConfirm={(reason) => {
              setDeclineOpen(false)
              void run(() => declineBooking(data.id, reason))
            }}
          />
          <ReasonDialog
            open={cancelOpen}
            title="Cancel this booking?"
            description="Cancellation is only allowed before work starts."
            confirmLabel="Cancel booking"
            label="Reason"
            danger
            busy={busy}
            onClose={() => setCancelOpen(false)}
            onConfirm={(reason) => {
              setCancelOpen(false)
              void run(() => cancelBooking(data.id, reason))
            }}
          />
        </div>
      ) : null}
    </WorkspaceLayout>
  )
}

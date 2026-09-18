import { useCallback, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { acceptOffer } from '../../api/offers'
import { ApiError } from '../../api/client'
import { cancelServiceRequest, getCustomerRequest } from '../../api/requests'
import type { RequestDetailForCustomer } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { OfferCard } from '../../components/OfferCard'
import { PageHeader } from '../../components/PageHeader'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatBudget, formatDate } from '../../utils/format'

export function CustomerRequestDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const requestId = Number(id)
  const notice = (location.state as { notice?: string } | null)?.notice
  const [data, setData] = useState<RequestDetailForCustomer | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [acceptConflict, setAcceptConflict] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const [acceptId, setAcceptId] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    if (Number.isNaN(requestId)) {
      setError('Request not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getCustomerRequest(requestId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this request.')
    } finally {
      setLoading(false)
    }
  }, [requestId])

  useEffect(() => {
    void load()
  }, [load])

  async function handleCancel() {
    if (!data) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      setData(await cancelServiceRequest(data.id))
      setCancelOpen(false)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not cancel the request.')
    } finally {
      setBusy(false)
    }
  }

  async function handleAccept() {
    if (acceptId === null) {
      return
    }

    setBusy(true)
    setActionError(null)
    setAcceptConflict(false)
    try {
      const booking = await acceptOffer(acceptId)
      navigate(`/customer/bookings/${booking.id}`)
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message)
        setAcceptConflict(err.status === 409)
        setAcceptId(null)
      } else {
        setActionError('Could not accept the offer.')
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Customer}>
      {loading ? <LoadingState label="Loading request" count={2} /> : null}
      {error ? (
        <ErrorState title="Unable to load request" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data ? (
        <>
          <PageHeader
            eyebrow={`${data.categoryName} · ${data.serviceName}`}
            title={data.title}
            description={data.description}
            actions={<StatusBadge status={data.status} />}
          />
          {notice ? (
            <div className="alert alert-success" role="status">
              {notice}
            </div>
          ) : null}
          {actionError ? (
            <div className="alert" role="alert">
              <p>{actionError}</p>
              {acceptConflict ? (
                <div className="dashboard-actions">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => {
                      setActionError(null)
                      setAcceptConflict(false)
                      void load()
                    }}
                  >
                    Reload offers
                  </Button>
                </div>
              ) : null}
            </div>
          ) : null}
          <dl className="detail-grid">
            <div>
              <dt>City</dt>
              <dd>{data.city}</dd>
            </div>
            <div>
              <dt>Preferred date</dt>
              <dd>{formatDate(data.preferredDate)}</dd>
            </div>
            <div>
              <dt>Budget</dt>
              <dd>{formatBudget(data.budgetMin, data.budgetMax)}</dd>
            </div>
            <div>
              <dt>Offers</dt>
              <dd>{data.offerCount}</dd>
            </div>
          </dl>
          <div className="dashboard-actions">
            {data.canEdit ? (
              <Button to={`/customer/requests/${data.id}/edit`} variant="secondary">
                Edit
              </Button>
            ) : null}
            {data.canCancel ? (
              <Button variant="ghost" onClick={() => setCancelOpen(true)}>
                Cancel request
              </Button>
            ) : null}
            {data.bookingId ? (
              <Button to={`/customer/bookings/${data.bookingId}`}>View booking</Button>
            ) : null}
          </div>
          <section className="dashboard-panel">
            <div className="dashboard-panel-head">
              <h2>Offers</h2>
            </div>
            {data.offers.length === 0 ? (
              <EmptyState
                title="No offers yet"
                description="Eligible providers in your city will see this request."
              />
            ) : (
              <div className="card-grid">
                {data.offers.map((offer) => (
                  <OfferCard
                    key={offer.id}
                    offer={offer}
                    accepting={busy && acceptId === offer.id}
                    onAccept={(id) => setAcceptId(id)}
                  />
                ))}
              </div>
            )}
          </section>
          <ConfirmDialog
            open={cancelOpen}
            title="Cancel this request?"
            description="Pending offers will be rejected and providers will no longer see this request."
            confirmLabel="Cancel request"
            danger
            busy={busy}
            onClose={() => setCancelOpen(false)}
            onConfirm={() => void handleCancel()}
          />
          <ConfirmDialog
            open={acceptId !== null}
            title="Accept this offer?"
            description="This creates a booking and rejects the other pending offers."
            confirmLabel="Accept offer"
            busy={busy}
            onClose={() => setAcceptId(null)}
            onConfirm={() => void handleAccept()}
          />
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

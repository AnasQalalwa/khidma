import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { ApiError } from '../../api/client'
import { getProviderRequest } from '../../api/requests'
import type { RequestDetailForProvider } from '../../api/types'
import { Roles } from '../../auth/roles'
import { OfferForm } from '../../components/OfferForm'
import { OfferSnapshotCard } from '../../components/OfferCard'
import { PageHeader } from '../../components/PageHeader'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatBudget, formatDate } from '../../utils/format'

export function ProviderRequestDetailPage() {
  const { id } = useParams()
  const requestId = Number(id)
  const [data, setData] = useState<RequestDetailForProvider | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (Number.isNaN(requestId)) {
      setError('Request not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getProviderRequest(requestId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this request.')
    } finally {
      setLoading(false)
    }
  }, [requestId])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Provider}>
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
          </dl>
          {data.myOffer ? (
            <section className="dashboard-panel">
              <h2>Your offer</h2>
              <OfferSnapshotCard offer={data.myOffer} />
            </section>
          ) : null}
          {data.canOffer ? (
            <section className="dashboard-panel">
              <h2>Submit an offer</h2>
              <OfferForm requestId={data.id} onSubmitted={() => void load()} />
            </section>
          ) : null}
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

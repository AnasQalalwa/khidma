import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getPublicProvider } from '../api/providers'
import type { PublicProvider } from '../api/types'
import { Button } from '../components/Button'
import { ProviderAvatar } from '../components/ProviderAvatar'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { WorkingHoursSummary } from '../components/schedule/WorkingHoursSummary'
import { formatDate, formatRating } from '../utils/format'

export function PublicProviderPage() {
  const { id } = useParams()
  const providerId = Number(id)
  const [data, setData] = useState<PublicProvider | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (Number.isNaN(providerId)) {
      setError('Provider not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getPublicProvider(providerId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this provider.')
    } finally {
      setLoading(false)
    }
  }, [providerId])

  useEffect(() => {
    void load()
  }, [load])

  if (loading) {
    return <LoadingState label="Loading provider" count={2} />
  }

  if (error) {
    return (
      <ErrorState title="Unable to load provider" description={error} onRetry={() => void load()} />
    )
  }

  if (!data) {
    return null
  }

  return (
    <>
      <section className="public-provider-hero">
        <ProviderAvatar
          providerId={data.id}
          name={data.fullName}
          hasPhoto={data.hasPhoto}
          size="lg"
        />
        <div>
          <p className="muted">{data.city}</p>
          <h1>{data.fullName}</h1>
          <div className="provider-hero-badges">
            <StatusBadge status={data.isVerified ? 'Approved' : 'PendingReview'} />
            <p className="muted" style={{ margin: 0 }}>
              {formatRating(data.averageRating, data.reviewCount)} · {data.yearsOfExperience} years
            </p>
          </div>
          <p>{data.bio ?? 'Local Khidma provider.'}</p>
          <WorkingHoursSummary hours={data.workingHours ?? []} />
        </div>
      </section>
      <section className="dashboard-panel">
        <h2>Services</h2>
        {data.services.length === 0 ? (
          <EmptyState
            title="No services listed"
            description="This provider has not chosen services yet."
          />
        ) : (
          <ul className="plain-list">
            {data.services.map((service) => (
              <li key={service.id} className="plain-row">
                <div>
                  <strong>{service.name}</strong>
                  <p className="muted">{service.categoryName}</p>
                </div>
                <Button to={`/book/${data.id}/${service.id}`} size="sm">
                  Book
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>
      <section className="dashboard-panel">
        <h2>Recent reviews</h2>
        {data.recentReviews.length === 0 ? (
          <EmptyState
            title="No reviews yet"
            description="Completed jobs will appear here after customers leave a rating."
          />
        ) : (
          <ul className="plain-list">
            {data.recentReviews.map((review, index) => (
              <li key={`${review.createdAt}-${index}`} className="plain-row">
                <div>
                  <strong>
                    {review.reviewerFirstName} · {review.rating}/5
                  </strong>
                  {review.comment ? <p>{review.comment}</p> : null}
                  <p className="muted">{formatDate(review.createdAt)}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  )
}

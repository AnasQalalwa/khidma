import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getPublicProvider } from '../api/providers'
import type { PublicProvider } from '../api/types'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
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
      <PageHeader
        eyebrow={data.city}
        title={data.fullName}
        description={data.bio ?? 'Local Khidma provider.'}
        actions={
          <StatusBadge status={data.isVerified ? 'Approved' : 'PendingReview'} />
        }
      />
      <p className="lead">{formatRating(data.averageRating, data.reviewCount)}</p>
      <p className="muted">{data.yearsOfExperience} years of experience</p>
      <section className="dashboard-panel">
        <h2>Services</h2>
        {data.services.length === 0 ? (
          <p className="muted">No services listed yet.</p>
        ) : (
          <ul className="chip-list">
            {data.services.map((service) => (
              <li key={service.id}>
                <Link to={`/catalog?category=${service.categoryId}`}>{service.name}</Link>
              </li>
            ))}
          </ul>
        )}
      </section>
      <section className="dashboard-panel">
        <h2>Recent reviews</h2>
        {data.recentReviews.length === 0 ? (
          <EmptyState title="No reviews yet" description="Completed jobs will appear here after customers leave a rating." />
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

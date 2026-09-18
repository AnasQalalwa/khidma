import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getAdminUser } from '../../api/admin'
import { ApiError } from '../../api/client'
import type { AdminUserDetail } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate, formatRating } from '../../utils/format'

export function AdminUserDetailPage() {
  const { userId } = useParams()
  const [data, setData] = useState<AdminUserDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!userId) {
      setError('User not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getAdminUser(userId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this user.')
    } finally {
      setLoading(false)
    }
  }, [userId])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <PageHeader
        eyebrow="User monitoring"
        title={data?.fullName ?? 'User'}
        description="Account activity and recent audit events. Identity secrets are never shown."
        actions={
          <Button to="/admin/users" variant="secondary">
            Back to users
          </Button>
        }
      />
      {loading ? <LoadingState label="Loading user" /> : null}
      {error ? (
        <ErrorState title="Unable to load user" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data ? (
        <section className="dashboard-panel">
          <p>
            <strong>{data.email}</strong> · {data.role}
          </p>
          <p className="muted">
            Created {formatDate(data.createdAt)} · Last login {formatDate(data.lastLoginAt)}
          </p>
          {data.city ? <p>City: {data.city}</p> : null}
          {data.role === 'Customer' ? (
            <p>
              Requests {data.requestCount} · Bookings {data.bookingCount} · Reviews{' '}
              {data.reviewCount}
            </p>
          ) : null}
          {data.role === 'Provider' ? (
            <>
              <div className="inline-actions">
                <StatusBadge
                  status={data.isSuspended ? 'Suspended' : data.verificationStatus ?? 'PendingReview'}
                />
                {data.providerProfileId ? (
                  <Link to={`/admin/verifications/${data.providerProfileId}`}>
                    Open verification
                  </Link>
                ) : null}
              </div>
              {data.isSuspended && data.suspensionReason ? (
                <p>Suspension reason: {data.suspensionReason}</p>
              ) : null}
              <p>{formatRating(data.averageRating ?? 0, data.reviewCount ?? 0)}</p>
              <p>
                Offers {data.offerCount} · Active bookings {data.activeBookingCount} · Completed{' '}
                {data.completedBookingCount}
              </p>
              <p className="muted">{data.services.join(', ') || 'No services listed'}</p>
            </>
          ) : null}
          <h2>Recent audit events</h2>
          {data.recentAuditEvents.length === 0 ? (
            <EmptyState
              title="No recent events"
              description="This account has no audit history yet."
            />
          ) : (
            <ul className="plain-list">
              {data.recentAuditEvents.map((event) => (
                <li key={event.id} className="plain-row">
                  <div>
                    <strong>{event.action}</strong>
                    <p className="muted">{formatDate(event.createdAt)}</p>
                  </div>
                  <StatusBadge status={event.outcome} />
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}
    </WorkspaceLayout>
  )
}

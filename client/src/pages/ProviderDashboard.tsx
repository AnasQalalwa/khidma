import { useCallback, useEffect, useState } from 'react'
import { Briefcase, CalendarCheck, Inbox, Search, Star, UserRound } from 'lucide-react'
import { ApiError } from '../api/client'
import { getProviderDashboard } from '../api/dashboard'
import type { ProviderDashboard as ProviderDashboardData } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Roles } from '../auth/roles'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { RequestCard } from '../components/RequestCard'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { WorkspaceLayout } from '../components/WorkspaceLayout'
import { formatDate, formatMoney } from '../utils/format'

export function ProviderDashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<ProviderDashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await getProviderDashboard())
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load dashboard.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Provider}>
      <DashboardShell
        badge={<RoleBadge role="Provider" />}
        title={`Welcome back, ${user?.fullName ?? 'Provider'}`}
        description="See eligible requests, manage offers, and keep jobs moving."
      >
        {loading ? <LoadingState label="Loading dashboard" /> : null}
        {error ? (
          <ErrorState title="Unable to load dashboard" description={error} onRetry={() => void load()} />
        ) : null}
        {!loading && !error && data ? (
          <>
            {!data.isApproved ? (
              <div className="alert" role="status">
                Your profile is pending admin approval. You will see matching requests after approval.
              </div>
            ) : null}
            <div className="dash-grid">
              <StatCard
                title="Eligible requests"
                value={data.eligibleRequestCount}
                icon={Inbox}
                accent="home"
              />
              <StatCard
                title="Pending offers"
                value={data.pendingOfferCount}
                icon={Briefcase}
                accent="technology"
              />
              <StatCard
                title="Active jobs"
                value={data.activeBookingCount}
                icon={CalendarCheck}
                accent="cleaning"
              />
              <StatCard
                title="Reviews"
                value={data.reviewCount}
                hint={data.reviewCount === 0 ? 'No reviews yet' : `Average ${data.averageRating.toFixed(2)}`}
                icon={Star}
                accent="education"
              />
            </div>
            <div className="dashboard-actions">
              <Button to="/provider/requests">View requests</Button>
              <Button to="/provider/offers" variant="secondary">
                My offers
              </Button>
              <Button to="/provider/bookings" variant="secondary">
                My bookings
              </Button>
              <Button to="/provider/profile" variant="ghost" icon={UserRound}>
                Edit profile
              </Button>
              <Button to="/catalog" variant="ghost" icon={Search}>
                Browse catalog
              </Button>
            </div>
            <DashboardPanel title="Available requests">
              {data.recentAvailableRequests.length === 0 ? (
                <EmptyState
                  title="No matching requests"
                  description="When customers in your city need your services, they will appear here."
                />
              ) : (
                <div className="card-grid">
                  {data.recentAvailableRequests.map((request) => (
                    <RequestCard
                      key={request.id}
                      request={request}
                      href={`/provider/requests/${request.id}`}
                    />
                  ))}
                </div>
              )}
            </DashboardPanel>
            <DashboardPanel title="My offers">
              {data.recentOffers.length === 0 ? (
                <p className="muted">You have not submitted any offers yet.</p>
              ) : (
                <ul className="plain-list">
                  {data.recentOffers.map((offer) => (
                    <li key={offer.id} className="plain-row">
                      <div>
                        <strong>{offer.requestTitle}</strong>
                        <p className="muted">
                          {formatMoney(offer.price)} · {formatDate(offer.estimatedDate)}
                        </p>
                      </div>
                      <StatusBadge status={offer.status} />
                    </li>
                  ))}
                </ul>
              )}
            </DashboardPanel>
            <DashboardPanel title="Active jobs">
              {data.activeJobs.length === 0 ? (
                <p className="muted">No active jobs right now.</p>
              ) : (
                <ul className="plain-list">
                  {data.activeJobs.map((job) => (
                    <li key={job.id} className="plain-row">
                      <div>
                        <strong>{job.title}</strong>
                        <p className="muted">
                          {job.counterpartyName} · {formatDate(job.scheduledDate)}
                        </p>
                      </div>
                      <StatusBadge status={job.status} />
                      <Button to={`/provider/bookings/${job.id}`} size="sm" variant="secondary">
                        View
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </DashboardPanel>
          </>
        ) : null}
      </DashboardShell>
    </WorkspaceLayout>
  )
}

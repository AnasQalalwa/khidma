import { useCallback, useEffect, useState } from 'react'
import { CalendarCheck, FileText, Inbox, Plus, Search, Star } from 'lucide-react'
import { ApiError } from '../api/client'
import { getCustomerDashboard } from '../api/dashboard'
import type { CustomerDashboard as CustomerDashboardData } from '../api/types'
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

export function CustomerDashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<CustomerDashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await getCustomerDashboard())
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
    <WorkspaceLayout role={Roles.Customer}>
      <DashboardShell
        badge={<RoleBadge role="Customer" />}
        title={`Welcome back, ${user?.fullName ?? 'Customer'}`}
        description="Create requests, compare offers, and manage bookings from one place."
      >
        {loading ? <LoadingState label="Loading dashboard" /> : null}
        {error ? (
          <ErrorState title="Unable to load dashboard" description={error} onRetry={() => void load()} />
        ) : null}
        {!loading && !error && data ? (
          <>
            <div className="dash-grid">
              <StatCard
                title="Open requests"
                value={data.openRequestCount}
                icon={FileText}
                accent="home"
              />
              <StatCard
                title="Offers awaiting decision"
                value={data.offersAwaitingDecision}
                icon={Inbox}
                accent="technology"
              />
              <StatCard
                title="Active bookings"
                value={data.activeBookingCount}
                icon={CalendarCheck}
                accent="cleaning"
              />
              <StatCard
                title="Awaiting review"
                value={data.completedAwaitingReview}
                icon={Star}
                accent="education"
              />
            </div>
            <div className="dashboard-actions">
              <Button to="/customer/requests/new" icon={Plus}>
                Create request
              </Button>
              <Button to="/catalog" variant="secondary" icon={Search}>
                Browse services
              </Button>
              <Button to="/customer/bookings" variant="ghost">
                View bookings
              </Button>
            </div>
            <DashboardPanel title="My requests" description="Your most recent service requests.">
              {data.recentRequests.length === 0 ? (
                <EmptyState
                  title="No service requests yet."
                  description="Create a request to start receiving offers."
                  action={
                    <Button to="/customer/requests/new" icon={Plus}>
                      Create a request
                    </Button>
                  }
                />
              ) : (
                <div className="card-grid">
                  {data.recentRequests.map((request) => (
                    <RequestCard
                      key={request.id}
                      request={request}
                      href={`/customer/requests/${request.id}`}
                      showOffers
                    />
                  ))}
                </div>
              )}
            </DashboardPanel>
            <DashboardPanel title="Active bookings">
              {data.activeBookings.length === 0 ? (
                <p className="muted">No active bookings right now.</p>
              ) : (
                <ul className="plain-list">
                  {data.activeBookings.map((booking) => (
                    <li key={booking.id} className="plain-row">
                      <div>
                        <strong>{booking.title}</strong>
                        <p className="muted">
                          {booking.counterpartyName} · {formatDate(booking.scheduledDate)} ·{' '}
                          {formatMoney(booking.finalPrice)}
                        </p>
                      </div>
                      <StatusBadge status={booking.status} />
                      <Button to={`/customer/bookings/${booking.id}`} size="sm" variant="secondary">
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

import { useCallback, useEffect, useState } from 'react'
import { CalendarCheck, Inbox, Search, Star, UserRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getProviderDashboard } from '../api/dashboard'
import { getMySchedule } from '../api/schedule'
import type { ProviderDashboard as ProviderDashboardData, ScheduleEntry } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Roles } from '../auth/roles'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { WorkspaceLayout } from '../components/WorkspaceLayout'
import { formatDay, formatMoney, formatSlot, formatTime } from '../utils/format'
import { addDays, toDateInput } from '../utils/hours'

export function ProviderDashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<ProviderDashboardData | null>(null)
  const [today, setToday] = useState<ScheduleEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const todayKey = toDateInput(new Date())
      const [dashboard, schedule] = await Promise.all([
        getProviderDashboard(),
        getMySchedule(todayKey, toDateInput(addDays(new Date(), 1))).catch(() => null),
      ])
      setData(dashboard)
      setToday(
        (schedule?.items ?? []).filter(
          (item) =>
            item.start &&
            item.status !== 'Pending' &&
            toDateInput(new Date(item.start)) === todayKey,
        ),
      )
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
        description="Respond to booking requests and keep accepted jobs moving."
      >
        {loading ? <LoadingState label="Loading dashboard" /> : null}
        {error ? (
          <ErrorState title="Unable to load dashboard" description={error} onRetry={() => void load()} />
        ) : null}
        {!loading && !error && data ? (
          <>
            {data.isSuspended ? (
              <div className="alert" role="alert">
                Your account is suspended
                {data.suspensionReason ? `: ${data.suspensionReason}` : '.'} Pending booking requests
                are declined. Scheduled and in-progress jobs stay available so you can finish them.
              </div>
            ) : null}
            {!data.isSuspended && data.verificationStatus !== 'Approved' ? (
              <div className="alert" role="status">
                {data.verificationStatus === 'Rejected'
                  ? 'Professional verification was rejected. Upload updated documents from '
                  : 'Professional verification is pending. Upload a license or certificate from '}
                <Link to="/provider/profile#verification">Profile → Professional verification</Link>
                {data.verificationStatus === 'Rejected'
                  ? ', then wait for admin review.'
                  : ' so an admin can review it before you receive new work.'}
              </div>
            ) : null}
            <div className="dash-grid">
              <StatCard
                title="Pending requests"
                value={data.pendingRequestCount}
                icon={Inbox}
                accent="home"
              />
              <StatCard
                title="Active jobs"
                value={data.activeJobCount}
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
            <DashboardPanel title="Today's schedule">
              {today.length === 0 ? (
                <p className="muted">Nothing scheduled for today.</p>
              ) : (
                <ul className="plain-list">
                  {today.map((item) => (
                    <li key={item.bookingId} className="plain-row">
                      <div>
                        <strong>{item.serviceName}</strong>
                        <p className="muted">
                          {formatTime(item.start)}–{formatTime(item.end)} · {item.customerName}
                        </p>
                      </div>
                      <Button to={`/provider/bookings/${item.bookingId}`} size="sm" variant="secondary">
                        Open
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
              <Button to="/provider/schedule" variant="ghost" size="sm">
                Open schedule
              </Button>
            </DashboardPanel>
            <div className="dashboard-actions">
              <Button to="/provider/bookings">View bookings</Button>
              <Button to="/provider/profile" variant="ghost" icon={UserRound}>
                Edit profile
              </Button>
              <Button to="/catalog" variant="ghost" icon={Search}>
                Browse catalog
              </Button>
            </div>
            <DashboardPanel title="Pending requests">
              {data.recentPendingRequests.length === 0 ? (
                <EmptyState
                  title="No pending requests"
                  description="When a customer books one of your services, the request appears here."
                />
              ) : (
                <ul className="plain-list">
                  {data.recentPendingRequests.map((booking) => (
                    <li key={booking.id} className="plain-row">
                      <div>
                        <strong>{booking.serviceName}</strong>
                        <p className="muted">
                          {booking.counterpartyName} · {formatDay(booking.requestedDate)}
                        </p>
                      </div>
                      <StatusBadge status={booking.status} />
                      <Button to={`/provider/bookings/${booking.id}`} size="sm" variant="secondary">
                        Review
                      </Button>
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
                        <strong>{job.serviceName}</strong>
                        <p className="muted">
                          {job.counterpartyName} ·{' '}
                          {job.scheduledStart
                            ? formatSlot(job.scheduledStart, job.scheduledEnd)
                            : formatDay(job.requestedDate)}{' '}
                          ·{' '}
                          {formatMoney(job.quotedPrice)}
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

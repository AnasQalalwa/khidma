import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Activity,
  ClipboardCheck,
  ShieldAlert,
  ShieldCheck,
  Users,
} from 'lucide-react'
import { getAdminAttention, getAdminStats } from '../api/admin'
import { getAuditLogs } from '../api/audit'
import { ApiError } from '../api/client'
import type { AdminAttention, AdminStats, AuditLogItem } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Roles } from '../auth/roles'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { WorkspaceLayout } from '../components/WorkspaceLayout'
import { formatDate } from '../utils/format'

export function AdminDashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<AdminStats | null>(null)
  const [attention, setAttention] = useState<AdminAttention | null>(null)
  const [recent, setRecent] = useState<AuditLogItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [stats, queue, logs] = await Promise.all([
        getAdminStats(),
        getAdminAttention(),
        getAuditLogs({ page: 1, pageSize: 10 }),
      ])
      setData(stats)
      setAttention(queue)
      setRecent(logs.items)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load admin stats.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <DashboardShell
        variant="admin"
        badge={<RoleBadge role="Admin" />}
        title="Platform monitoring"
        description={`${user?.fullName ?? 'Admin'} — verification, moderation, and audit activity.`}
      >
        {loading ? <LoadingState label="Loading admin overview" /> : null}
        {error ? (
          <ErrorState title="Unable to load overview" description={error} onRetry={() => void load()} />
        ) : null}
        {!loading && !error && data ? (
          <>
            <div className="dash-grid">
              <StatCard title="Total users" value={data.totalUsers} icon={Users} accent="home" />
              <StatCard title="Customers" value={data.customers} icon={Users} accent="education" />
              <StatCard title="Providers" value={data.providers} icon={ClipboardCheck} accent="technology" />
              <StatCard
                title="Pending verification"
                value={data.pendingVerification}
                icon={Activity}
                accent="education"
              />
              <StatCard
                title="Approved providers"
                value={data.approvedProviders}
                icon={ShieldCheck}
                accent="cleaning"
              />
              <StatCard
                title="Suspended providers"
                value={data.suspendedProviders}
                icon={ShieldAlert}
                accent="home"
              />
              <StatCard title="Open requests" value={data.openRequests} icon={Activity} accent="service" />
              <StatCard
                title="Active bookings"
                value={data.activeBookings}
                icon={Activity}
                accent="home"
              />
              <StatCard
                title="Completed bookings"
                value={data.completedBookings}
                icon={ClipboardCheck}
                accent="cleaning"
              />
              <StatCard
                title="Audit events — last 24h"
                value={data.auditEventsLast24h}
                icon={Activity}
                accent="technology"
              />
            </div>
            <div className="dashboard-actions">
              <Button to="/admin/verifications">Review verifications</Button>
              <Button to="/admin/users" variant="secondary">
                Monitor users
              </Button>
              <Button to="/admin/audit" variant="ghost">
                View audit logs
              </Button>
            </div>
            <DashboardPanel
              title="Needs attention"
              description="Open verification, document, and moderation items."
            >
              {attention && attention.items.length > 0 ? (
                <ul className="attention-list">
                  {attention.items.map((item) => (
                    <li key={item.kind}>
                      <Link className="attention-link" to={item.href}>
                        <strong>{item.title}</strong>
                        <p className="muted">{item.detail}</p>
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : (
                <EmptyState
                  title="Nothing needs attention"
                  description="No verification or moderation items are waiting."
                />
              )}
            </DashboardPanel>
            <DashboardPanel title="Recent activity" description="Latest security and marketplace events.">
              {recent.length === 0 ? (
                <EmptyState
                  title="No audit events yet"
                  description="Security and marketplace actions will show up here."
                />
              ) : (
                <ul className="plain-list">
                  {recent.map((event) => (
                    <li key={event.id} className="plain-row">
                      <div>
                        <strong>{event.action}</strong>
                        <p className="muted">
                          {formatDate(event.createdAt)} · {event.actorEmail ?? 'System'} ·{' '}
                          {event.entityType ?? '—'}
                        </p>
                      </div>
                      <StatusBadge status={event.outcome} />
                    </li>
                  ))}
                </ul>
              )}
              <div className="dashboard-actions">
                <Button to="/admin/audit" variant="secondary">
                  View all audit logs
                </Button>
              </div>
            </DashboardPanel>
          </>
        ) : null}
      </DashboardShell>
    </WorkspaceLayout>
  )
}

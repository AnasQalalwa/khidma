import { useCallback, useEffect, useState } from 'react'
import {
  Activity,
  Layers,
  LayoutGrid,
  Search,
  Shield,
  Users,
} from 'lucide-react'
import { getAdminStats } from '../api/admin'
import { ApiError } from '../api/client'
import type { AdminStats } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Roles } from '../auth/roles'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'
import { ErrorState, LoadingState } from '../components/States'
import { WorkspaceLayout } from '../components/WorkspaceLayout'

export function AdminDashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<AdminStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await getAdminStats())
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
        title="Platform Administration"
        description={`${user?.fullName ?? 'Admin'} — approve providers, maintain the catalog, and monitor activity.`}
      >
        {loading ? <LoadingState label="Loading admin stats" /> : null}
        {error ? (
          <ErrorState title="Unable to load stats" description={error} onRetry={() => void load()} />
        ) : null}
        {!loading && !error && data ? (
          <>
            <div className="dash-grid">
              <StatCard title="Customers" value={data.customers} icon={Users} accent="home" />
              <StatCard title="Providers" value={data.providers} icon={Shield} accent="technology" />
              <StatCard
                title="Pending providers"
                value={data.pendingProviders}
                icon={Activity}
                accent="education"
              />
              <StatCard title="Categories" value={data.categories} icon={Layers} accent="education" />
              <StatCard title="Services" value={data.services} icon={LayoutGrid} accent="cleaning" />
              <StatCard
                title="Open requests"
                value={data.openRequests}
                icon={Activity}
                accent="service"
              />
              <StatCard
                title="Active bookings"
                value={data.activeBookings}
                icon={Activity}
                accent="home"
              />
              <StatCard
                title="Completed bookings"
                value={data.completedBookings}
                icon={Activity}
                accent="cleaning"
              />
            </div>
            <div className="dashboard-actions">
              <Button to="/admin/providers">Manage providers</Button>
              <Button to="/admin/catalog" variant="secondary">
                Manage catalog
              </Button>
              <Button to="/catalog" variant="ghost" icon={Search}>
                View catalog
              </Button>
            </div>
            <DashboardPanel
              title="Provider management"
              description="Approve providers before they can see matching customer requests."
            />
            <DashboardPanel
              title="Catalog management"
              description="Add, rename, or remove categories and services when they are not in use."
            />
          </>
        ) : null}
      </DashboardShell>
    </WorkspaceLayout>
  )
}

import { Activity, Layers, LayoutGrid, Search, Shield, Users } from 'lucide-react'
import { useAuth } from '../auth/useAuth'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'

export function AdminDashboard() {
  const { user } = useAuth()

  return (
    <DashboardShell
      variant="admin"
      badge={<RoleBadge role="Admin" />}
      title="Platform Administration"
      description={`${user?.fullName ?? 'Admin'} — later weeks will add provider approval, catalog management, and platform oversight.`}
    >
      <div className="dash-grid">
        <StatCard title="Providers" hint="No data yet" icon={Shield} accent="technology" />
        <StatCard title="Customers" hint="No data yet" icon={Users} accent="home" />
        <StatCard title="Categories" hint="Coming soon" icon={Layers} accent="education" />
        <StatCard title="Services" hint="Coming soon" icon={LayoutGrid} accent="cleaning" />
        <StatCard title="Platform Activity" hint="Coming soon" icon={Activity} accent="service" />
      </div>
      <div className="dashboard-actions">
        <Button to="/catalog" icon={Search} variant="secondary">
          View catalog
        </Button>
      </div>
      <DashboardPanel
        title="Provider management"
        description="Approve and review providers from this panel in a later week."
        comingSoon
      />
      <DashboardPanel
        title="Catalog management"
        description="Create and maintain categories and services here in a later week."
        comingSoon
      />
      <DashboardPanel
        title="Platform overview"
        description="Operational activity and health summaries are future work."
        comingSoon
      />
    </DashboardShell>
  )
}

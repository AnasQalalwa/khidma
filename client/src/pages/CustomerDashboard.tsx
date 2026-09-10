import { CalendarCheck, FileText, Inbox, Search, Star } from 'lucide-react'
import { useAuth } from '../auth/useAuth'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'

export function CustomerDashboard() {
  const { user } = useAuth()

  return (
    <DashboardShell
      badge={<RoleBadge role="Customer" />}
      title={`Welcome back, ${user?.fullName ?? 'Customer'}`}
      description="Create requests, compare offers, and manage bookings in later weeks. For now you can browse the live catalog."
    >
      <div className="dash-grid">
        <StatCard title="My Requests" hint="Coming in Week 2" icon={FileText} accent="home" />
        <StatCard title="Offers Received" hint="Coming in Week 2" icon={Inbox} accent="technology" />
        <StatCard title="Bookings" hint="No data yet" icon={CalendarCheck} accent="cleaning" />
        <StatCard title="Reviews" hint="Coming soon" icon={Star} accent="education" />
      </div>
      <div className="dashboard-actions">
        <Button to="/catalog" icon={Search}>
          Browse services
        </Button>
      </div>
      <DashboardPanel
        title="Create a request"
        description="Service requests will be available in a later week. This action is not available yet."
        comingSoon
      />
    </DashboardShell>
  )
}

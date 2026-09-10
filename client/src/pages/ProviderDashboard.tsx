import { Briefcase, CalendarCheck, Inbox, Search, Star } from 'lucide-react'
import { useAuth } from '../auth/useAuth'
import { RoleBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { DashboardPanel, DashboardShell } from '../components/DashboardShell'
import { StatCard } from '../components/StatCard'

export function ProviderDashboard() {
  const { user } = useAuth()

  return (
    <DashboardShell
      badge={<RoleBadge role="Provider" />}
      title={`Welcome back, ${user?.fullName ?? 'Provider'}`}
      description="Eligible requests, offers, and booking progress will appear here in later weeks."
    >
      <div className="dash-grid">
        <StatCard
          title="Available Requests"
          hint="Coming in Week 2"
          icon={Inbox}
          accent="home"
        />
        <StatCard
          title="My Offers"
          hint="Coming in Week 2"
          icon={Briefcase}
          accent="technology"
        />
        <StatCard
          title="Active Bookings"
          hint="No data yet"
          icon={CalendarCheck}
          accent="cleaning"
        />
        <StatCard title="Reviews" hint="Coming soon" icon={Star} accent="education" />
      </div>
      <div className="dashboard-actions">
        <Button to="/catalog" icon={Search}>
          Browse catalog
        </Button>
      </div>
      <DashboardPanel
        title="Complete your provider profile"
        description="Profile editing and additional provider details are upcoming functionality."
        comingSoon
      />
      <DashboardPanel
        title="Upcoming functionality"
        description="Eligible request lists, offer submission, and job progress will be added in later weeks."
        comingSoon
      />
    </DashboardShell>
  )
}

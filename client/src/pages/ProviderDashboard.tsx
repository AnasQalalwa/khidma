import { useAuth } from '../auth/useAuth'
import { DashboardStatCard } from '../components/CategoryCard'

export function ProviderDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard-shell">
      <div className="dashboard dashboard-hero">
        <div>
          <span className="badge">Provider</span>
          <h1>Hello, {user?.fullName}</h1>
          <p className="muted">
            Later weeks will show eligible requests, your offers, and booking
            progress.
          </p>
        </div>
      </div>
      <div className="dash-grid">
        <DashboardStatCard title="Available Requests" hint="Coming soon" />
        <DashboardStatCard title="My Offers" hint="Coming soon" />
        <DashboardStatCard title="Active Jobs" hint="Coming soon" />
      </div>
    </section>
  )
}

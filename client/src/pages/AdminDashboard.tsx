import { useAuth } from '../auth/useAuth'
import { DashboardStatCard } from '../components/CategoryCard'

export function AdminDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard-shell">
      <div className="dashboard dashboard-hero">
        <div>
          <span className="badge">Admin</span>
          <h1>Hello, {user?.fullName}</h1>
          <p className="muted">
            Later weeks will add provider approval, catalog management, and
            platform oversight.
          </p>
        </div>
      </div>
      <div className="dash-grid">
        <DashboardStatCard title="Providers" hint="Coming soon" />
        <DashboardStatCard title="Services" hint="Coming soon" />
        <DashboardStatCard title="Platform Activity" hint="Coming soon" />
      </div>
    </section>
  )
}

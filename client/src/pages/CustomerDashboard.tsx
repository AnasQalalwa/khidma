import { useAuth } from '../auth/AuthContext'
import { DashboardStatCard } from '../components/CategoryCard'

export function CustomerDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard-shell">
      <div className="dashboard dashboard-hero">
        <div>
          <span className="badge">Customer</span>
          <h1>Hello, {user?.fullName}</h1>
          <p className="muted">
            Later weeks will let you create service requests, compare provider
            offers, and manage bookings.
          </p>
        </div>
      </div>
      <div className="dash-grid">
        <DashboardStatCard title="Active Requests" hint="Coming soon" />
        <DashboardStatCard title="Offers" hint="Coming soon" />
        <DashboardStatCard title="Bookings" hint="Coming soon" />
      </div>
    </section>
  )
}

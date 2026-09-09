import { useAuth } from '../auth/AuthContext'

export function CustomerDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard">
      <span className="badge">Customer</span>
      <h1>Hello, {user?.fullName}</h1>
      <p>
        Later weeks will let you create service requests, compare provider
        offers, and manage bookings.
      </p>
    </section>
  )
}

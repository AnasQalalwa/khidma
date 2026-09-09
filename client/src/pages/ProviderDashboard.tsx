import { useAuth } from '../auth/AuthContext'

export function ProviderDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard">
      <span className="badge">Provider</span>
      <h1>Hello, {user?.fullName}</h1>
      <p>
        Later weeks will show eligible requests, your offers, and booking
        progress.
      </p>
    </section>
  )
}

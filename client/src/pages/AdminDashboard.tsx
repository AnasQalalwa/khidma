import { useAuth } from '../auth/AuthContext'

export function AdminDashboard() {
  const { user } = useAuth()

  return (
    <section className="dashboard">
      <span className="badge">Admin</span>
      <h1>Hello, {user?.fullName}</h1>
      <p>
        Later weeks will add provider approval, catalog management, and
        platform oversight.
      </p>
    </section>
  )
}

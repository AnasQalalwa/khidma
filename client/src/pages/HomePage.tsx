import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'

export function HomePage() {
  const { authenticated, user } = useAuth()

  return (
    <section className="hero">
      <h1>Book trusted local services</h1>
      <p className="lead">
        Khidma connects customers with eligible providers. Create a service
        request, compare offers, confirm a booking, and leave a review when
        the work is complete.
      </p>
      <div className="actions">
        <Link className="btn" to="/catalog">
          Browse catalog
        </Link>
        {authenticated && user ? (
          <Link className="btn secondary" to={dashboardPath(user.role)}>
            Go to dashboard
          </Link>
        ) : (
          <>
            <Link className="btn secondary" to="/register">
              Register
            </Link>
            <Link className="btn secondary" to="/login">
              Login
            </Link>
          </>
        )}
      </div>
    </section>
  )
}

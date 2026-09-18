import { ShieldAlert } from 'lucide-react'
import { useAuth } from '../auth/useAuth'
import { dashboardPath } from '../auth/roles'
import { Button } from '../components/Button'
import { IconTile } from '../components/icons'

export function ForbiddenPage() {
  const { authenticated, user } = useAuth()

  return (
    <section className="status-page status-block">
      <IconTile icon={ShieldAlert} accent="electrical" size={24} large />
      <h1>Access forbidden</h1>
      <p className="muted">
        You do not have permission to view that page. Sign in with the correct
        role, or return to a page you can use.
      </p>
      <div className="status-actions">
        <Button to="/">Back Home</Button>
        {authenticated && user ? (
          <Button variant="secondary" to={dashboardPath(user.role)}>
            Dashboard
          </Button>
        ) : null}
      </div>
    </section>
  )
}

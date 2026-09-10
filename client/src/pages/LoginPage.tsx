import { useState, type FormEvent } from 'react'
import { Mail } from 'lucide-react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { dashboardPath } from '../auth/roles'
import { AuthShell } from '../components/AuthShell'
import { Button } from '../components/Button'
import { FormField } from '../components/FormField'
import { PasswordField } from '../components/PasswordField'

export function LoginPage() {
  const { authenticated, user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (authenticated && user) {
    return <Navigate to={dashboardPath(user.role)} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)

    try {
      const current = await login({ email, password })
      const from = (location.state as { from?: string } | null)?.from
      navigate(from && from !== '/login' ? from : dashboardPath(current.role), {
        replace: true,
      })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Login failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthShell
      eyebrow="Welcome back"
      title="Sign in to manage requests, offers, and bookings."
      description="Your session is restored from a secure HttpOnly cookie."
      points={[
        'Role-based dashboards for customers, providers, and admins',
        'Protected booking workflow coming in later weeks',
        'One account, one trusted marketplace',
      ]}
    >
      <div>
        <span className="eyebrow">Account</span>
        <h1>Login</h1>
        <p className="muted">Use your Khidma email and password.</p>
      </div>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        {error ? (
          <div className="alert" role="alert">
            {error}
          </div>
        ) : null}
        <FormField label="Email" icon={Mail}>
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </FormField>
        <PasswordField
          label="Password"
          value={password}
          onChange={setPassword}
          autoComplete="current-password"
          required
        />
        <Button type="submit" block loading={submitting}>
          {submitting ? 'Signing in…' : 'Login'}
        </Button>
      </form>
      <p className="muted auth-footer">
        No account? <Link to="/register">Register</Link>
      </p>
    </AuthShell>
  )
}

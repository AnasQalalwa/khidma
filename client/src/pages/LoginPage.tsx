import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { dashboardPath } from '../auth/roles'
import { AuthCard } from '../components/CategoryCard'
import { Button } from '../components/Button'

export function LoginPage() {
  const { authenticated, user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
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
    <div className="auth-shell">
      <div className="auth-visual">
        <span className="eyebrow">Welcome back</span>
        <h2>Sign in to manage requests, offers, and bookings.</h2>
        <p>Your session is restored from a secure HttpOnly cookie.</p>
      </div>
      <div className="auth-panel">
        <AuthCard
          title="Login"
          description="Use your Khidma email and password."
        >
          <form className="form" onSubmit={(event) => void handleSubmit(event)}>
            {error ? <div className="alert" role="alert">{error}</div> : null}
            <label className="field">
              <span>Email</span>
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </label>
            <label className="field">
              <span>Password</span>
              <div className="password-field">
                <input
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                />
                <button
                  type="button"
                  className="password-toggle"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  onClick={() => setShowPassword((value) => !value)}
                >
                  {showPassword ? 'Hide' : 'Show'}
                </button>
              </div>
            </label>
            <Button type="submit" block disabled={submitting}>
              {submitting ? 'Signing in…' : 'Login'}
            </Button>
          </form>
          <p className="muted">
            No account? <Link to="/register">Register</Link>
          </p>
        </AuthCard>
      </div>
    </div>
  )
}

import { useState, type FormEvent } from 'react'
import {
  ArrowRight,
  CalendarCheck,
  Mail,
  ShieldCheck,
  Users,
} from 'lucide-react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError, fieldError } from '../api/client'
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
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  if (authenticated && user) {
    return <Navigate to={dashboardPath(user.role)} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      const current = await login({ email, password })
      const from = (location.state as { from?: string } | null)?.from
      navigate(from && from !== '/login' ? from : dashboardPath(current.role), {
        replace: true,
      })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setError('Login failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthShell
      variant="login"
      eyebrow="Welcome back"
      title={
        <>
          <span>Sign in to manage</span>
          <span>requests, offers,</span>
          <span>and bookings.</span>
        </>
      }
      description="Continue to your Khidma account and keep things moving."
      benefits={[
        {
          icon: Users,
          title: 'Role-based dashboards',
          description: 'Manage the experience that matches your account.',
        },
        {
          icon: CalendarCheck,
          title: 'Protected booking workflow',
          description: 'Your activity stays organized in one place.',
        },
        {
          icon: ShieldCheck,
          title: 'One account, one trusted marketplace',
          description: 'Simple. Secure. Reliable.',
        },
      ]}
    >
      <div className="auth-card-head">
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
        <FormField label="Email" icon={Mail} error={fieldError(fieldErrors, 'email')}>
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@example.com"
            required
          />
        </FormField>
        <PasswordField
          label="Password"
          value={password}
          onChange={setPassword}
          autoComplete="current-password"
          placeholder="Enter your password"
          required
          error={fieldError(fieldErrors, 'password')}
        />
        <Button type="submit" block loading={submitting} iconRight={ArrowRight}>
          {submitting ? 'Signing in…' : 'Login'}
        </Button>
      </form>
      <p className="muted auth-footer">
        No account? <Link to="/register">Register</Link>
      </p>
    </AuthShell>
  )
}

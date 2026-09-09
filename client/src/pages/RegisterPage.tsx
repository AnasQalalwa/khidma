import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError, fieldError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'
import { AuthCard } from '../components/CategoryCard'
import { Button } from '../components/Button'

type PublicRole = 'Customer' | 'Provider'

export function RegisterPage() {
  const { authenticated, user, register } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [role, setRole] = useState<PublicRole>('Customer')
  const [city, setCity] = useState('')
  const [yearsOfExperience, setYearsOfExperience] = useState('0')
  const [bio, setBio] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  if (authenticated && user) {
    return <Navigate to={dashboardPath(user.role)} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      const payload = {
        fullName,
        email,
        password,
        role,
        city,
        ...(role === 'Provider'
          ? {
              yearsOfExperience: Number(yearsOfExperience) || 0,
              bio: bio.trim() || undefined,
            }
          : {}),
      }

      const current = await register(payload)
      navigate(dashboardPath(current.role), { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setError('Registration failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-visual">
        <span className="eyebrow">Join Khidma</span>
        <h2>Create a customer or provider account in minutes.</h2>
        <p>Admin accounts are seeded for development and cannot be registered here.</p>
      </div>
      <div className="auth-panel">
        <AuthCard
          title="Register"
          description="Choose how you will use Khidma, then complete your profile."
        >
          <form className="form" onSubmit={(event) => void handleSubmit(event)}>
            {error ? <div className="alert" role="alert">{error}</div> : null}
            <div className="field">
              <span>Account type</span>
              <div className="role-grid">
                <button
                  type="button"
                  className="role-choice"
                  aria-pressed={role === 'Customer'}
                  onClick={() => setRole('Customer')}
                >
                  <strong>Customer</strong>
                  <span>Request services and compare offers.</span>
                </button>
                <button
                  type="button"
                  className="role-choice"
                  aria-pressed={role === 'Provider'}
                  onClick={() => setRole('Provider')}
                >
                  <strong>Provider</strong>
                  <span>Offer work and manage bookings.</span>
                </button>
              </div>
              {fieldError(fieldErrors, 'role') ? (
                <span className="field-error">{fieldError(fieldErrors, 'role')}</span>
              ) : null}
            </div>
            <label className="field">
              <span>Full name</span>
              <input
                value={fullName}
                onChange={(event) => setFullName(event.target.value)}
                required
              />
              {fieldError(fieldErrors, 'fullName') ? (
                <span className="field-error">{fieldError(fieldErrors, 'fullName')}</span>
              ) : null}
            </label>
            <label className="field">
              <span>Email</span>
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
              {fieldError(fieldErrors, 'email') ? (
                <span className="field-error">{fieldError(fieldErrors, 'email')}</span>
              ) : null}
            </label>
            <label className="field">
              <span>Password</span>
              <div className="password-field">
                <input
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
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
              {fieldError(fieldErrors, 'password') ? (
                <span className="field-error">{fieldError(fieldErrors, 'password')}</span>
              ) : null}
            </label>
            <label className="field">
              <span>City</span>
              <input
                value={city}
                onChange={(event) => setCity(event.target.value)}
                required
              />
              {fieldError(fieldErrors, 'city') ? (
                <span className="field-error">{fieldError(fieldErrors, 'city')}</span>
              ) : null}
            </label>
            {role === 'Provider' ? (
              <div className="provider-fields">
                <label className="field">
                  <span>Years of experience</span>
                  <input
                    type="number"
                    min={0}
                    max={80}
                    value={yearsOfExperience}
                    onChange={(event) => setYearsOfExperience(event.target.value)}
                  />
                </label>
                <label className="field">
                  <span>Bio</span>
                  <textarea
                    rows={3}
                    value={bio}
                    onChange={(event) => setBio(event.target.value)}
                  />
                </label>
              </div>
            ) : null}
            <Button type="submit" block disabled={submitting}>
              {submitting ? 'Creating account…' : 'Create account'}
            </Button>
          </form>
          <p className="muted">
            Already registered? <Link to="/login">Login</Link>
          </p>
        </AuthCard>
      </div>
    </div>
  )
}

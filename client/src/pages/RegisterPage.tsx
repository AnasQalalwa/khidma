import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError, fieldError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'

type PublicRole = 'Customer' | 'Provider'

export function RegisterPage() {
  const { authenticated, user, register } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
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
    <section className="card">
      <h1 className="page-title">Register</h1>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        {error ? <div className="alert">{error}</div> : null}
        <label>
          Full name
          <input
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            required
          />
          {fieldError(fieldErrors, 'fullName') ? (
            <span className="field-error">{fieldError(fieldErrors, 'fullName')}</span>
          ) : null}
        </label>
        <label>
          Email
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
        <label>
          Password
          <input
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          {fieldError(fieldErrors, 'password') ? (
            <span className="field-error">{fieldError(fieldErrors, 'password')}</span>
          ) : null}
        </label>
        <label>
          Role
          <select
            value={role}
            onChange={(event) => setRole(event.target.value as PublicRole)}
          >
            <option value="Customer">Customer</option>
            <option value="Provider">Provider</option>
          </select>
          {fieldError(fieldErrors, 'role') ? (
            <span className="field-error">{fieldError(fieldErrors, 'role')}</span>
          ) : null}
        </label>
        <label>
          City
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
          <>
            <label>
              Years of experience
              <input
                type="number"
                min={0}
                max={80}
                value={yearsOfExperience}
                onChange={(event) => setYearsOfExperience(event.target.value)}
              />
            </label>
            <label>
              Bio
              <textarea
                rows={3}
                value={bio}
                onChange={(event) => setBio(event.target.value)}
              />
            </label>
          </>
        ) : null}
        <button className="btn" type="submit" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>
      <p className="muted">
        Already registered? <Link to="/login">Login</Link>
      </p>
    </section>
  )
}

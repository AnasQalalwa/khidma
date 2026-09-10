import { useState, type FormEvent } from 'react'
import { Mail, MapPin, User } from 'lucide-react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError, fieldError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { dashboardPath } from '../auth/roles'
import { AuthShell } from '../components/AuthShell'
import { Button } from '../components/Button'
import { FormField } from '../components/FormField'
import { PasswordField } from '../components/PasswordField'
import { RoleSelector, type PublicRole } from '../components/RoleSelector'

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
    <AuthShell
      eyebrow="Join Khidma"
      title="Create a customer or provider account in minutes."
      description="Admin accounts are seeded for development and cannot be registered here."
      points={[
        'Customers request services and compare offers',
        'Providers offer work in approved categories',
        'The same secure account model for every role',
      ]}
    >
      <div>
        <span className="eyebrow">Get started</span>
        <h1>Register</h1>
        <p className="muted">
          Choose how you will use Khidma, then complete your profile.
        </p>
      </div>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        {error ? (
          <div className="alert" role="alert">
            {error}
          </div>
        ) : null}

        <div className="form-section">
          <h2 className="form-section-title">Your account</h2>
          <FormField
            label="Full name"
            icon={User}
            error={fieldError(fieldErrors, 'fullName')}
          >
            <input
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              autoComplete="name"
              required
            />
          </FormField>
          <FormField
            label="Email"
            icon={Mail}
            error={fieldError(fieldErrors, 'email')}
          >
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
            autoComplete="new-password"
            required
            error={fieldError(fieldErrors, 'password')}
          />
          <FormField
            label="City"
            icon={MapPin}
            error={fieldError(fieldErrors, 'city')}
          >
            <input
              value={city}
              onChange={(event) => setCity(event.target.value)}
              autoComplete="address-level2"
              required
            />
          </FormField>
        </div>

        <div className="form-section">
          <RoleSelector
            value={role}
            onChange={setRole}
            error={fieldError(fieldErrors, 'role')}
          />
        </div>

        {role === 'Provider' ? (
          <div className="form-section provider-fields">
            <h2 className="form-section-title">Provider details</h2>
            <FormField label="Years of experience">
              <input
                type="number"
                min={0}
                max={80}
                value={yearsOfExperience}
                onChange={(event) => setYearsOfExperience(event.target.value)}
              />
            </FormField>
            <FormField label="Bio">
              <textarea
                rows={3}
                value={bio}
                onChange={(event) => setBio(event.target.value)}
              />
            </FormField>
          </div>
        ) : null}

        <Button type="submit" block loading={submitting}>
          {submitting ? 'Creating account…' : 'Create account'}
        </Button>
      </form>
      <p className="muted auth-footer">
        Already registered? <Link to="/login">Login</Link>
      </p>
    </AuthShell>
  )
}

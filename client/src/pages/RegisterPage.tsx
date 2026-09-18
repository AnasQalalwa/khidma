import { useState, type FormEvent } from 'react'
import {
  ChartNoAxesColumnIncreasing,
  Mail,
  MapPin,
  ShieldCheck,
  UserRound,
  UsersRound,
} from 'lucide-react'
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
    if (submitting) {
      return
    }

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
      variant="register"
      eyebrow="Get started"
      title={
        <>
          <span>Create a customer or</span>
          <span>provider account</span>
          <span>in minutes.</span>
        </>
      }
      description="Join Khidma and be part of a trusted local marketplace where people find services, offer their skills, and build stronger communities."
      benefits={[
        {
          icon: UsersRound,
          title: 'Access real opportunities',
          description: 'Request services or offer your skills.',
        },
        {
          icon: ShieldCheck,
          title: 'A secure and trusted platform',
          description: 'Your account is protected throughout the experience.',
        },
        {
          icon: ChartNoAxesColumnIncreasing,
          title: 'Grow with your community',
          description: 'More services. More connections.',
        },
      ]}
    >
      <div className="auth-card-head">
        <span className="eyebrow">Account</span>
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

        <FormField
          label="Full name"
          icon={UserRound}
          error={fieldError(fieldErrors, 'fullName')}
        >
          <input
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            autoComplete="name"
            placeholder="Enter your full name"
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
            placeholder="you@example.com"
            required
          />
        </FormField>
        <PasswordField
          label="Password"
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          placeholder="Create a password"
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
            placeholder="Enter your city"
            required
          />
        </FormField>

        <RoleSelector
          value={role}
          onChange={setRole}
          error={fieldError(fieldErrors, 'role')}
        />

        {role === 'Provider' ? (
          <div className="provider-fields">
            <FormField
              label="Years of experience"
              error={fieldError(fieldErrors, 'yearsOfExperience')}
            >
              <input
                type="number"
                min={0}
                max={80}
                value={yearsOfExperience}
                onChange={(event) => setYearsOfExperience(event.target.value)}
              />
            </FormField>
            <FormField
              label="Bio"
              error={fieldError(fieldErrors, 'bio')}
            >
              <textarea
                rows={3}
                value={bio}
                onChange={(event) => setBio(event.target.value)}
                placeholder="Tell customers about your work"
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

import { useState, type FormEvent } from 'react'
import {
  ChartNoAxesColumnIncreasing,
  Mail,
  MapPin,
  Phone,
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
import { CitySelect } from '../components/CitySelect'
import { FormField } from '../components/FormField'
import { PhoneInput } from '../components/PhoneInput'
import { PasswordField } from '../components/PasswordField'
import { RoleSelector, type PublicRole } from '../components/RoleSelector'
import { findCity } from '../data/cities'
import {
  isStrongPassword,
  isValidEmail,
  isValidPhone,
  PASSWORD_REQUIREMENT_MESSAGE,
  PHONE_REQUIREMENT_MESSAGE,
} from '../utils/validation'

const GENERIC_VALIDATION_TITLE = 'One or more validation errors occurred.'

export function RegisterPage() {
  const { authenticated, user, register } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<PublicRole>('Customer')
  const [city, setCity] = useState('')
  const [yearsOfExperience, setYearsOfExperience] = useState('0')
  const [bio, setBio] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  if (authenticated && user) {
    const next =
      user.role === 'Provider'
        ? '/provider/profile#verification'
        : dashboardPath(user.role)
    return <Navigate to={next} replace />
  }

  function validate(): Record<string, string[]> {
    const next: Record<string, string[]> = {}

    if (fullName.trim().length < 2) {
      next.fullName = ['Enter your full name.']
    }

    if (!isValidEmail(email)) {
      next.email = ['Enter a valid email like you@example.com.']
    }

    if (!isValidPhone(phoneNumber)) {
      next.phoneNumber = [PHONE_REQUIREMENT_MESSAGE]
    }

    if (!isStrongPassword(password)) {
      next.password = [PASSWORD_REQUIREMENT_MESSAGE]
    }

    if (!findCity(city)) {
      next.city = ['Choose your city.']
    }

    if (role === 'Provider') {
      const years = Number(yearsOfExperience)
      if (!Number.isInteger(years) || years < 0 || years > 80) {
        next.yearsOfExperience = ['Years of experience must be between 0 and 80.']
      }
    }

    return next
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    const localErrors = validate()
    if (Object.keys(localErrors).length > 0) {
      setFieldErrors(localErrors)
      setError(null)
      return
    }

    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      const payload = {
        fullName: fullName.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        password,
        role,
        city: city.trim(),
        ...(role === 'Provider'
          ? {
              yearsOfExperience: Number(yearsOfExperience) || 0,
              bio: bio.trim() || undefined,
            }
          : {}),
      }

      const current = await register(payload)
      const next =
        current.role === 'Provider'
          ? '/provider/profile#verification'
          : dashboardPath(current.role)
      navigate(next, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setFieldErrors(err.validationErrors)
        const hasFields = Object.keys(err.validationErrors).length > 0
        setError(
          hasFields && err.message === GENERIC_VALIDATION_TITLE ? null : err.message,
        )
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
      <form
        className="form"
        noValidate
        onSubmit={(event) => void handleSubmit(event)}
      >
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
          hint="Use an email like you@example.com."
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
        <FormField
          label="Phone number"
          icon={Phone}
          error={fieldError(fieldErrors, 'phoneNumber')}
          hint="Choose a country code, then type the local number. Palestine numbers look like 0598969367."
        >
          <PhoneInput value={phoneNumber} onChange={setPhoneNumber} required />
        </FormField>
        <PasswordField
          label="Password"
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          placeholder="Create a password"
          required
          showRules
          error={fieldError(fieldErrors, 'password')}
        />
        <FormField
          label="City"
          icon={MapPin}
          error={fieldError(fieldErrors, 'city')}
          hint="Choose the city where you live or work. Providers can pin an exact map location later on their profile."
        >
          <CitySelect value={city} onChange={setCity} required />
        </FormField>

        <RoleSelector
          value={role}
          onChange={setRole}
          error={fieldError(fieldErrors, 'role')}
        />

        {role === 'Provider' ? (
          <div className="provider-fields">
            <p className="auth-verify-note" role="note">
              After you create this account, open{' '}
              <strong>Profile → Professional verification</strong> and upload a
              license, certificate, or other proof (PDF, JPEG, or PNG). An admin
              reviews it before you can take new jobs.
            </p>
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

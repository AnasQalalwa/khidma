import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { changePassword, getAccountProfile, updateAccountProfile } from '../../api/account'
import { ApiError, fieldError } from '../../api/client'
import type { AccountProfile } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { CitySelect } from '../../components/CitySelect'
import { FormField } from '../../components/FormField'
import { PhoneInput } from '../../components/PhoneInput'
import { PageHeader } from '../../components/PageHeader'
import { PasswordField } from '../../components/PasswordField'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { findCity } from '../../data/cities'
import { formatPhone, isStrongPassword, isValidPhone, PASSWORD_REQUIREMENT_MESSAGE, PHONE_REQUIREMENT_MESSAGE } from '../../utils/validation'

export function CustomerProfilePage() {
  const { refreshUser } = useAuth()
  const [profile, setProfile] = useState<AccountProfile | null>(null)
  const [fullName, setFullName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [city, setCity] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [success, setSuccess] = useState<string | null>(null)
  const [savingProfile, setSavingProfile] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const account = await getAccountProfile()
      setProfile(account)
      setFullName(account.fullName)
      setPhoneNumber(formatPhone(account.phoneNumber))
      setCity(account.city ?? '')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load your profile.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function handleProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingProfile) {
      return
    }

    const next: Record<string, string[]> = {}
    if (fullName.trim().length < 2) {
      next.fullName = ['Enter your full name.']
    }
    if (!isValidPhone(phoneNumber)) {
      next.phoneNumber = [PHONE_REQUIREMENT_MESSAGE]
    }
    if (!findCity(city)) {
      next.city = ['Choose your city.']
    }
    if (Object.keys(next).length > 0) {
      setFieldErrors(next)
      setProfileError(null)
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setPasswordError(null)
    setFieldErrors({})
    setSuccess(null)
    try {
      const updated = await updateAccountProfile({
        fullName: fullName.trim(),
        phoneNumber: phoneNumber.trim(),
        city: city.trim(),
      })
      setProfile(updated)
      await refreshUser()
      setSuccess('Profile saved.')
    } catch (err) {
      if (err instanceof ApiError) {
        setProfileError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setProfileError('Could not save your profile.')
      }
    } finally {
      setSavingProfile(false)
    }
  }

  async function handlePassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingPassword) {
      return
    }

    const next: Record<string, string[]> = {}
    if (!currentPassword) {
      next.currentPassword = ['Enter your current password.']
    }
    if (!isStrongPassword(newPassword)) {
      next.newPassword = [PASSWORD_REQUIREMENT_MESSAGE]
    }
    if (Object.keys(next).length > 0) {
      setFieldErrors(next)
      setPasswordError(null)
      return
    }

    setSavingPassword(true)
    setPasswordError(null)
    setProfileError(null)
    setFieldErrors({})
    setSuccess(null)
    try {
      await changePassword({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setSuccess('Password updated.')
    } catch (err) {
      if (err instanceof ApiError) {
        setPasswordError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setPasswordError('Could not change your password.')
      }
    } finally {
      setSavingPassword(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Customer}>
      <PageHeader
        eyebrow="Account"
        title="Profile"
        description="Update how providers reach you, and review your booking history from My Bookings."
      />
      {loading ? <LoadingState label="Loading profile" /> : null}
      {error ? (
        <ErrorState title="Unable to load profile" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && profile ? (
        <>
          {success ? (
            <div className="alert" role="status">
              {success}
            </div>
          ) : null}
          <section className="dashboard-panel" id="contact">
            <h2>Contact details</h2>
            <p className="muted">
              Signed in as {profile.email}. Providers see your phone number only after you send a booking request.
            </p>
            <form className="form" onSubmit={(event) => void handleProfile(event)}>
              {profileError ? (
                <div className="alert" role="alert">
                  {profileError}
                </div>
              ) : null}
              <FormField label="Full name" error={fieldError(fieldErrors, 'fullName')}>
                <input
                  value={fullName}
                  onChange={(event) => setFullName(event.target.value)}
                  autoComplete="name"
                  required
                />
              </FormField>
              <FormField
                label="Phone number"
                error={fieldError(fieldErrors, 'phoneNumber')}
                hint="Choose a country code, then type the local number. Palestine numbers look like 0598969367."
              >
                <PhoneInput value={phoneNumber} onChange={setPhoneNumber} required />
              </FormField>
              <FormField
                label="City"
                error={fieldError(fieldErrors, 'city')}
                hint="Provider lists default to this city."
              >
                <CitySelect value={city} onChange={setCity} required />
              </FormField>
              <Button type="submit" loading={savingProfile}>
                {savingProfile ? 'Saving…' : 'Save profile'}
              </Button>
            </form>
          </section>
          <section className="dashboard-panel" id="password">
            <h2>Password</h2>
            <form className="form" onSubmit={(event) => void handlePassword(event)}>
              {passwordError ? (
                <div className="alert" role="alert">
                  {passwordError}
                </div>
              ) : null}
              <PasswordField
                label="Current password"
                value={currentPassword}
                onChange={setCurrentPassword}
                autoComplete="current-password"
                error={fieldError(fieldErrors, 'currentPassword')}
              />
              <PasswordField
                label="New password"
                value={newPassword}
                onChange={setNewPassword}
                autoComplete="new-password"
                showRules
                error={fieldError(fieldErrors, 'newPassword')}
              />
              <Button type="submit" variant="secondary" loading={savingPassword}>
                {savingPassword ? 'Updating…' : 'Change password'}
              </Button>
            </form>
          </section>
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

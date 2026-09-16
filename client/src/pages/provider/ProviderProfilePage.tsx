import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { getServices } from '../../api/catalog'
import { ApiError, fieldError } from '../../api/client'
import {
  getMyProviderProfile,
  replaceMyServices,
  updateMyProviderProfile,
} from '../../api/providers'
import type { CatalogService } from '../../api/catalog'
import type { ProviderMe } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatRating } from '../../utils/format'

export function ProviderProfilePage() {
  const [profile, setProfile] = useState<ProviderMe | null>(null)
  const [catalog, setCatalog] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [city, setCity] = useState('')
  const [years, setYears] = useState('0')
  const [bio, setBio] = useState('')
  const [serviceIds, setServiceIds] = useState<number[]>([])
  const [savingProfile, setSavingProfile] = useState(false)
  const [savingServices, setSavingServices] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [servicesError, setServicesError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [success, setSuccess] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [me, services] = await Promise.all([getMyProviderProfile(), getServices()])
      setProfile(me)
      setCatalog(services)
      setCity(me.city)
      setYears(String(me.yearsOfExperience))
      setBio(me.bio ?? '')
      setServiceIds(me.services.map((service) => service.id))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load profile.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function handleProfileSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingProfile) {
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setFieldErrors({})
    setSuccess(null)
    try {
      const updated = await updateMyProviderProfile({
        city,
        yearsOfExperience: Number(years) || 0,
        bio: bio.trim() || null,
      })
      setProfile(updated)
      setSuccess('Profile saved.')
    } catch (err) {
      if (err instanceof ApiError) {
        setProfileError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setProfileError('Could not save profile.')
      }
    } finally {
      setSavingProfile(false)
    }
  }

  async function handleServicesSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingServices) {
      return
    }

    setSavingServices(true)
    setServicesError(null)
    setSuccess(null)
    try {
      const updated = await replaceMyServices(serviceIds)
      setProfile(updated)
      setSuccess('Services updated.')
    } catch (err) {
      setServicesError(err instanceof ApiError ? err.message : 'Could not save services.')
    } finally {
      setSavingServices(false)
    }
  }

  function toggleService(id: number) {
    setServiceIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    )
  }

  return (
    <WorkspaceLayout role={Roles.Provider}>
      <PageHeader
        eyebrow="Provider"
        title="Profile"
        description="Keep your city, experience, and offered services current so you can see matching requests."
      />
      {loading ? <LoadingState label="Loading profile" count={2} /> : null}
      {error ? (
        <ErrorState title="Unable to load profile" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && profile ? (
        <>
          <div className="profile-summary">
            <StatusBadge status={profile.isApproved ? 'Approved' : 'Pending'} />
            <p>{formatRating(profile.averageRating, profile.reviewCount)}</p>
          </div>
          {success ? (
            <div className="alert alert-success" role="status">
              {success}
            </div>
          ) : null}
          <section className="dashboard-panel">
            <h2>Profile</h2>
            <form className="form" onSubmit={(event) => void handleProfileSubmit(event)}>
              {profileError ? (
                <div className="alert" role="alert">
                  {profileError}
                </div>
              ) : null}
              <FormField label="City" error={fieldError(fieldErrors, 'city')}>
                <input value={city} onChange={(event) => setCity(event.target.value)} required />
              </FormField>
              <FormField
                label="Years of experience"
                error={fieldError(fieldErrors, 'yearsOfExperience')}
              >
                <input
                  type="number"
                  min={0}
                  max={80}
                  value={years}
                  onChange={(event) => setYears(event.target.value)}
                />
              </FormField>
              <FormField label="Bio" error={fieldError(fieldErrors, 'bio')}>
                <textarea rows={4} value={bio} onChange={(event) => setBio(event.target.value)} />
              </FormField>
              <Button type="submit" loading={savingProfile}>
                {savingProfile ? 'Saving…' : 'Save profile'}
              </Button>
            </form>
          </section>
          <section className="dashboard-panel">
            <h2>Services offered</h2>
            <form className="form" onSubmit={(event) => void handleServicesSubmit(event)}>
              {servicesError ? (
                <div className="alert" role="alert">
                  {servicesError}
                </div>
              ) : null}
              <div className="check-grid">
                {catalog.map((service) => {
                  const checked = serviceIds.includes(service.id)
                  return (
                    <label
                      key={service.id}
                      className={checked ? 'check-card is-checked' : 'check-card'}
                    >
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggleService(service.id)}
                      />
                      <span>
                        <strong>{service.name}</strong>
                        <small>{service.categoryName}</small>
                      </span>
                    </label>
                  )
                })}
              </div>
              <Button type="submit" loading={savingServices}>
                {savingServices ? 'Saving…' : 'Save services'}
              </Button>
            </form>
          </section>
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

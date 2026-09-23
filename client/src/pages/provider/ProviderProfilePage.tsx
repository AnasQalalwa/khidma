import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { BadgeCheck, Briefcase, Lock, MapPin, Star } from 'lucide-react'
import { getServices } from '../../api/catalog'
import { ApiError, fieldError } from '../../api/client'
import {
  requestLocationChange,
  requestServiceAddition,
  getMyProviderProfile,
  replaceMyServices,
  updateMyProviderProfile,
  uploadMyPhoto,
} from '../../api/providers'
import {
  deleteVerificationDocument,
  downloadMyVerificationDocument,
  getMyVerification,
  uploadVerificationDocument,
} from '../../api/verification'
import type { CatalogService } from '../../api/catalog'
import type {
  ProviderMe,
  ProviderVerification,
  VerificationDocumentType,
} from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { CitySelect } from '../../components/CitySelect'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { DocumentList } from '../../components/DocumentList'
import { FormField } from '../../components/FormField'
import { PhoneInput } from '../../components/PhoneInput'
import { LocationMap } from '../../components/LocationMap'
import { ProviderAvatar } from '../../components/ProviderAvatar'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatRating } from '../../utils/format'
import { formatPhone, isValidPhone, PHONE_REQUIREMENT_MESSAGE } from '../../utils/validation'
import { findCity } from '../../data/cities'

const DOCUMENT_TYPES: { value: VerificationDocumentType; label: string }[] = [
  { value: 'ProfessionalCertificate', label: 'Professional certificate' },
  { value: 'ProfessionalLicense', label: 'Professional license' },
  { value: 'TrainingCertificate', label: 'Training certificate' },
  { value: 'PortfolioEvidence', label: 'Portfolio evidence' },
  { value: 'Other', label: 'Other professional proof' },
]

export function ProviderProfilePage() {
  const [profile, setProfile] = useState<ProviderMe | null>(null)
  const [catalog, setCatalog] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [city, setCity] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [latitude, setLatitude] = useState<number | null>(null)
  const [longitude, setLongitude] = useState<number | null>(null)
  const [years, setYears] = useState('0')
  const [bio, setBio] = useState('')
  const [serviceIds, setServiceIds] = useState<number[]>([])
  const [savingProfile, setSavingProfile] = useState(false)
  const [savingServices, setSavingServices] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [servicesError, setServicesError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [success, setSuccess] = useState<string | null>(null)
  const [verification, setVerification] = useState<ProviderVerification | null>(null)
  const [documentType, setDocumentType] = useState<VerificationDocumentType>('ProfessionalCertificate')
  const [documentFile, setDocumentFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<number | null>(null)
  const [photoBusy, setPhotoBusy] = useState(false)
  const [photoKey, setPhotoKey] = useState(() => Date.now())
  const [requestingLocation, setRequestingLocation] = useState(false)
  const [locationDraftCity, setLocationDraftCity] = useState('')
  const [locationDraftLat, setLocationDraftLat] = useState<number | null>(null)
  const [locationDraftLng, setLocationDraftLng] = useState<number | null>(null)
  const [addServiceId, setAddServiceId] = useState<number | ''>('')
  const [addDocumentType, setAddDocumentType] =
    useState<VerificationDocumentType>('ProfessionalCertificate')
  const [addFile, setAddFile] = useState<File | null>(null)
  const [removeServiceId, setRemoveServiceId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [me, services, mine] = await Promise.all([
        getMyProviderProfile(),
        getServices(),
        getMyVerification(),
      ])
      setProfile(me)
      setCatalog(services)
      setVerification(mine)
      setCity(me.city)
      setPhoneNumber(formatPhone(me.phoneNumber))
      setLatitude(me.latitude)
      setLongitude(me.longitude)
      setYears(String(me.yearsOfExperience))
      setBio(me.bio ?? '')
      setServiceIds(me.services.map((service) => service.id))
      setLocationDraftCity(me.city)
      setLocationDraftLat(me.latitude)
      setLocationDraftLng(me.longitude)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load profile.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    if (loading || error) {
      return
    }

    if (window.location.hash !== '#verification') {
      return
    }

    document.getElementById('verification')?.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    })
  }, [loading, error])

  const pendingLocation = profile?.pendingChanges.find((change) => change.type === 'Location')
  const pendingServices = profile?.pendingChanges.filter((change) => change.type === 'AddService') ?? []
  const offeredIds = useMemo(
    () => new Set(profile?.services.map((service) => service.id) ?? []),
    [profile],
  )
  const addableServices = catalog.filter(
    (service) =>
      !offeredIds.has(service.id) &&
      !pendingServices.some((change) => change.serviceId === service.id),
  )

  async function handleProfileSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingProfile || !profile) {
      return
    }

    if (!isValidPhone(phoneNumber)) {
      setFieldErrors({ phoneNumber: [PHONE_REQUIREMENT_MESSAGE] })
      setProfileError(null)
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setFieldErrors({})
    setSuccess(null)
    try {
      const updated = await updateMyProviderProfile({
        city: profile.canEditLocation ? city : profile.city,
        latitude: profile.canEditLocation ? latitude : profile.latitude,
        longitude: profile.canEditLocation ? longitude : profile.longitude,
        phoneNumber: phoneNumber.trim(),
        yearsOfExperience: Number(years) || 0,
        bio: bio.trim() || null,
      })
      applyProfile(updated)
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
      applyProfile(updated)
      setSuccess('Services updated.')
    } catch (err) {
      setServicesError(err instanceof ApiError ? err.message : 'Could not save services.')
    } finally {
      setSavingServices(false)
    }
  }

  function applyProfile(updated: ProviderMe) {
    setProfile(updated)
    setCity(updated.city)
    setPhoneNumber(formatPhone(updated.phoneNumber))
    setLatitude(updated.latitude)
    setLongitude(updated.longitude)
    setYears(String(updated.yearsOfExperience))
    setBio(updated.bio ?? '')
    setServiceIds(updated.services.map((service) => service.id))
  }

  function toggleService(id: number) {
    setServiceIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    )
  }

  async function handlePhoto(file: File) {
    setPhotoBusy(true)
    setProfileError(null)
    setSuccess(null)
    try {
      applyProfile(await uploadMyPhoto(file))
      setPhotoKey(Date.now())
      setSuccess('Profile photo updated.')
    } catch (err) {
      setProfileError(err instanceof ApiError ? err.message : 'Could not save this photo.')
    } finally {
      setPhotoBusy(false)
    }
  }

  async function handleLocationRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!requestingLocation) {
      return
    }

    setSavingProfile(true)
    setProfileError(null)
    setSuccess(null)
    try {
      applyProfile(
        await requestLocationChange({
          city: locationDraftCity,
          latitude: locationDraftLat,
          longitude: locationDraftLng,
        }),
      )
      setRequestingLocation(false)
      setSuccess('Location change sent for admin review. Your current city stays live until it is approved.')
    } catch (err) {
      setProfileError(err instanceof ApiError ? err.message : 'Could not request this location change.')
    } finally {
      setSavingProfile(false)
    }
  }

  async function handleAddService(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!addServiceId || !addFile) {
      setServicesError('Choose a service and upload proof that you can offer it.')
      return
    }

    setSavingServices(true)
    setServicesError(null)
    setSuccess(null)
    try {
      applyProfile(await requestServiceAddition(addServiceId, addDocumentType, addFile))
      setVerification(await getMyVerification())
      setAddServiceId('')
      setAddFile(null)
      setSuccess('Service request sent. An admin will review your proof before customers can book it.')
    } catch (err) {
      setServicesError(err instanceof ApiError ? err.message : 'Could not request this service.')
    } finally {
      setSavingServices(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Provider}>
      {loading ? <LoadingState label="Loading profile" count={2} /> : null}
      {error ? (
        <ErrorState title="Unable to load profile" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && profile ? (
        <div className="provider-profile">
          <section className="provider-hero">
            <div className="provider-hero-body">
              <ProviderAvatar
                providerId={profile.id}
                name={profile.fullName}
                hasPhoto={profile.hasPhoto}
                cacheKey={photoKey}
                editable
                uploading={photoBusy}
                onUpload={(file) => void handlePhoto(file)}
              />
              <div className="provider-hero-copy">
                <p className="provider-hero-eyebrow">Your public profile</p>
                <h1>{profile.fullName}</h1>
                <p className="provider-hero-meta">
                  <span>
                    <MapPin size={16} /> {profile.city}
                  </span>
                  <span>
                    <Star size={16} /> {formatRating(profile.averageRating, profile.reviewCount)}
                  </span>
                  <span>
                    <Briefcase size={16} /> {profile.yearsOfExperience} years
                  </span>
                </p>
                <div className="provider-hero-badges">
                  <StatusBadge status={profile.isSuspended ? 'Suspended' : profile.verificationStatus} />
                  {profile.verificationStatus === 'Approved' ? (
                    <span className="provider-trust-chip">
                      <BadgeCheck size={16} /> Verified professional
                    </span>
                  ) : (
                    <span className="provider-trust-chip is-pending">Waiting for admin review</span>
                  )}
                </div>
                <p className="provider-hero-bio">
                  {profile.bio || 'Add a short bio so customers know who they are hiring.'}
                </p>
              </div>
            </div>
          </section>

          {profile.isSuspended ? (
            <div className="alert" role="alert">
              This account is suspended
              {profile.suspensionReason ? `: ${profile.suspensionReason}` : '.'}
            </div>
          ) : null}
          {profile.verificationStatus === 'Rejected' && profile.verificationRejectionReason ? (
            <div className="alert" role="status">
              Verification was rejected: {profile.verificationRejectionReason}
            </div>
          ) : null}
          {success ? (
            <div className="alert alert-success" role="status">
              {success}
            </div>
          ) : null}

          <div className="provider-profile-grid">
            <section className="dashboard-panel" id="about">
              <h2>About you</h2>
              <form className="form" onSubmit={(event) => void handleProfileSubmit(event)}>
                {profileError ? (
                  <div className="alert" role="alert">
                    {profileError}
                  </div>
                ) : null}
                {profile.canEditLocation ? (
                  <>
                    <FormField
                      label="City"
                      error={fieldError(fieldErrors, 'city')}
                      hint="Customers in this city can book you."
                    >
                      <CitySelect
                        value={city}
                        required
                        onChange={(next) => {
                          setCity(next)
                          const known = findCity(next)
                          if (known) {
                            setLatitude(known.lat)
                            setLongitude(known.lng)
                          }
                        }}
                      />
                    </FormField>
                    <div className="field">
                      <span>Map location</span>
                      <LocationMap
                        city={city}
                        latitude={latitude}
                        longitude={longitude}
                        onChange={(next) => {
                          setCity(next.city)
                          setLatitude(next.latitude)
                          setLongitude(next.longitude)
                        }}
                      />
                    </div>
                  </>
                ) : null}
                <FormField
                  label="Phone number"
                  error={fieldError(fieldErrors, 'phoneNumber')}
                  hint="Choose a country code, then type the local number. Customers see it only after they book you."
                >
                  <PhoneInput value={phoneNumber} onChange={setPhoneNumber} required />
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

            {profile.canEditLocation ? null : (
              <section className="dashboard-panel">
                <div className="dashboard-panel-head">
                  <h2 id="location">Work location</h2>
                  <span className="lock-hint">
                    <Lock size={14} /> Approved
                  </span>
                </div>
                <p className="muted">
                  Customers find you by this city. Changing it needs admin approval so matching stays honest.
                </p>
                {pendingLocation ? (
                  <div className="alert" role="status">
                    Waiting for review: move to {pendingLocation.requestedCity}.
                  </div>
                ) : null}
                <LocationMap
                  key={`live-${profile.city}`}
                  city={profile.city}
                  latitude={profile.latitude}
                  longitude={profile.longitude}
                  readOnly
                />
                {pendingLocation ? null : requestingLocation ? (
                  <form className="form" onSubmit={(event) => void handleLocationRequest(event)}>
                    <FormField label="Requested city">
                      <CitySelect
                        value={locationDraftCity}
                        required
                        onChange={(next) => {
                          setLocationDraftCity(next)
                          const known = findCity(next)
                          if (known) {
                            setLocationDraftLat(known.lat)
                            setLocationDraftLng(known.lng)
                          }
                        }}
                      />
                    </FormField>
                    <LocationMap
                      key="draft"
                      city={locationDraftCity}
                      latitude={locationDraftLat}
                      longitude={locationDraftLng}
                      onChange={(next) => {
                        setLocationDraftCity(next.city)
                        setLocationDraftLat(next.latitude)
                        setLocationDraftLng(next.longitude)
                      }}
                    />
                    <div className="inline-actions">
                      <Button type="submit" loading={savingProfile}>
                        Submit for approval
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        onClick={() => setRequestingLocation(false)}
                      >
                        Cancel
                      </Button>
                    </div>
                  </form>
                ) : (
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => {
                      setLocationDraftCity(profile.city)
                      setLocationDraftLat(profile.latitude)
                      setLocationDraftLng(profile.longitude)
                      setRequestingLocation(true)
                    }}
                  >
                    Request a new location
                  </Button>
                )}
              </section>
            )}

            <section className="dashboard-panel">
              <div className="dashboard-panel-head">
                <h2 id="services">Services offered</h2>
                {profile.canEditServices ? null : (
                  <span className="lock-hint">
                    <Lock size={14} /> Proof required to add
                  </span>
                )}
              </div>
              {servicesError ? (
                <div className="alert" role="alert">
                  {servicesError}
                </div>
              ) : null}
              {profile.canEditServices ? (
                <form className="form" onSubmit={(event) => void handleServicesSubmit(event)}>
                  <p className="muted">
                    Choose the work you can do. After approval, adding another service needs a document that
                    proves it.
                  </p>
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
              ) : (
                <>
                  <ul className="profile-service-list">
                    {profile.services.map((service) => (
                      <li key={service.id}>
                        <div>
                          <strong>{service.name}</strong>
                          <small>{service.categoryName}</small>
                        </div>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => setRemoveServiceId(service.id)}
                        >
                          Stop offering
                        </Button>
                      </li>
                    ))}
                    {pendingServices.map((change) => (
                      <li key={change.id} className="is-pending">
                        <div>
                          <strong>{change.serviceName}</strong>
                          <small>Waiting for admin review of {change.proofFileName}</small>
                        </div>
                        <StatusBadge status="Pending" />
                      </li>
                    ))}
                  </ul>
                  {profile.services.length === 0 && pendingServices.length === 0 ? (
                    <p className="muted">You are not offering any live services yet.</p>
                  ) : null}
                  <form className="form" onSubmit={(event) => void handleAddService(event)}>
                    <h3>Add another service</h3>
                    <p className="muted">
                      This is the only upload that adds a service. Attach proof for that service. It stays
                      off your profile until an admin approves the file.
                    </p>
                    <FormField label="Service">
                      <select
                        value={addServiceId}
                        onChange={(event) =>
                          setAddServiceId(event.target.value ? Number(event.target.value) : '')
                        }
                      >
                        <option value="">Choose a service</option>
                        {addableServices.map((service) => (
                          <option key={service.id} value={service.id}>
                            {service.name}
                          </option>
                        ))}
                      </select>
                    </FormField>
                    <FormField label="Proof type">
                      <select
                        value={addDocumentType}
                        onChange={(event) =>
                          setAddDocumentType(event.target.value as VerificationDocumentType)
                        }
                      >
                        {DOCUMENT_TYPES.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </FormField>
                    <FormField label="Proof file">
                      <input
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                        onChange={(event) => setAddFile(event.target.files?.[0] ?? null)}
                      />
                    </FormField>
                    <Button type="submit" loading={savingServices}>
                      {savingServices ? 'Sending…' : 'Request this service'}
                    </Button>
                  </form>
                </>
              )}
            </section>

            <section className="dashboard-panel" id="verification">
              <h2>{profile.canEditServices ? 'Account verification' : 'Documents on file'}</h2>
              <p className="muted">
                {profile.canEditServices
                  ? 'Upload the certificate or license an admin needs before your account can receive work. This does not add a service. You choose services in the list above.'
                  : 'Files already sent for your account. A new service is added only from Add another service, with its own proof.'}
              </p>
              {uploadError ? (
                <div className="alert" role="alert">
                  {uploadError}
                </div>
              ) : null}
              {profile.canEditServices ? (
                <form
                  className="form"
                  onSubmit={(event) => {
                    event.preventDefault()
                    if (!documentFile || uploading) {
                      setUploadError('Choose a PDF, JPEG, or PNG file first.')
                      return
                    }
                    void (async () => {
                      setUploading(true)
                      setUploadError(null)
                      setSuccess(null)
                      try {
                        const updated = await uploadVerificationDocument(documentType, documentFile)
                        setVerification(updated)
                        setDocumentFile(null)
                        setSuccess('Document uploaded for account review.')
                      } catch (err) {
                        setUploadError(
                          err instanceof ApiError ? err.message : 'Could not upload this document.',
                        )
                      } finally {
                        setUploading(false)
                      }
                    })()
                  }}
                >
                  <FormField label="Document type">
                    <select
                      value={documentType}
                      onChange={(event) =>
                        setDocumentType(event.target.value as VerificationDocumentType)
                      }
                    >
                      {DOCUMENT_TYPES.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </FormField>
                  <FormField label="File">
                    <input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                      onChange={(event) => setDocumentFile(event.target.files?.[0] ?? null)}
                    />
                  </FormField>
                  <Button type="submit" loading={uploading}>
                    {uploading ? 'Uploading…' : 'Upload for account review'}
                  </Button>
                </form>
              ) : null}
              {verification ? (
                <DocumentList
                  documents={verification.documents}
                  empty="No professional documents uploaded yet."
                  renderActions={(document) => (
                    <>
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() =>
                          void downloadMyVerificationDocument(
                            document.id,
                            document.originalFileName,
                          )
                        }
                      >
                        Download
                      </Button>
                      {document.reviewStatus === 'Pending' ? (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => setDeleteTarget(document.id)}
                        >
                          Delete
                        </Button>
                      ) : null}
                    </>
                  )}
                />
              ) : null}
            </section>
          </div>

          <ConfirmDialog
            open={deleteTarget !== null}
            title="Delete this document?"
            description="Only pending documents can be removed. Approved or rejected files stay in the review history."
            confirmLabel="Delete document"
            danger
            busy={uploading}
            onConfirm={() => {
              if (deleteTarget == null) {
                return
              }
              void (async () => {
                setUploading(true)
                setUploadError(null)
                try {
                  const updated = await deleteVerificationDocument(deleteTarget)
                  setVerification(updated)
                  setDeleteTarget(null)
                  setSuccess('Pending document deleted.')
                } catch (err) {
                  setUploadError(
                    err instanceof ApiError ? err.message : 'Could not delete this document.',
                  )
                } finally {
                  setUploading(false)
                }
              })()
            }}
            onClose={() => setDeleteTarget(null)}
          />
          <ConfirmDialog
            open={removeServiceId !== null}
            title="Stop offering this service?"
            description="Customers will no longer be able to book you for it. Adding it back later needs fresh proof and admin approval."
            confirmLabel="Stop offering"
            danger
            busy={savingServices}
            onConfirm={() => {
              if (removeServiceId == null || !profile) {
                return
              }
              void (async () => {
                setSavingServices(true)
                setServicesError(null)
                try {
                  applyProfile(
                    await replaceMyServices(
                      profile.services
                        .map((service) => service.id)
                        .filter((id) => id !== removeServiceId),
                    ),
                  )
                  setRemoveServiceId(null)
                  setSuccess('Service removed from your live profile.')
                } catch (err) {
                  setServicesError(
                    err instanceof ApiError ? err.message : 'Could not remove this service.',
                  )
                } finally {
                  setSavingServices(false)
                }
              })()
            }}
            onClose={() => setRemoveServiceId(null)}
          />
        </div>
      ) : null}
    </WorkspaceLayout>
  )
}

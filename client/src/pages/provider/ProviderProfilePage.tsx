import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { getServices } from '../../api/catalog'
import { ApiError, fieldError } from '../../api/client'
import {
  getMyProviderProfile,
  replaceMyServices,
  updateMyProviderProfile,
} from '../../api/providers'
import {
  deleteVerificationDocument,
  downloadMyVerificationDocument,
  getMyVerification,
  uploadVerificationDocument,
} from '../../api/verification'
import type { CatalogService } from '../../api/catalog'
import type { ProviderMe, ProviderVerification, VerificationDocumentType } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { DocumentList } from '../../components/DocumentList'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatRating } from '../../utils/format'

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
            <StatusBadge status={profile.isSuspended ? 'Suspended' : profile.verificationStatus} />
            <p>{formatRating(profile.averageRating, profile.reviewCount)}</p>
          </div>
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
          <section className="dashboard-panel">
            <h2>Professional verification</h2>
            <p className="muted">
              Upload PDF, JPEG, or PNG professional proof up to 10 MB. Admin review is required
              before you can receive new work.
            </p>
            {uploadError ? (
              <div className="alert" role="alert">
                {uploadError}
              </div>
            ) : null}
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
                    setSuccess('Document uploaded for review.')
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
                {uploading ? 'Uploading…' : 'Upload document'}
              </Button>
            </form>
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
                      <Button size="sm" variant="ghost" onClick={() => setDeleteTarget(document.id)}>
                        Delete
                      </Button>
                    ) : null}
                  </>
                )}
              />
            ) : null}
          </section>
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
        </>
      ) : null}
    </WorkspaceLayout>
  )
}

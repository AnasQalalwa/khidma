import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createBooking } from '../api/bookings'
import { getService } from '../api/catalog'
import { ApiError, fieldError } from '../api/client'
import { getPublicProvider } from '../api/providers'
import { getProviderAvailability } from '../api/schedule'
import type { CatalogService } from '../api/catalog'
import type { BusyInterval, PublicProvider, WorkingHour } from '../api/types'
import { Button } from '../components/Button'
import { FormField } from '../components/FormField'
import { PageHeader } from '../components/PageHeader'
import { ProviderAvatar } from '../components/ProviderAvatar'
import { DayPicker } from '../components/schedule/DayPicker'
import { WorkingHoursSummary } from '../components/schedule/WorkingHoursSummary'
import { ErrorState, LoadingState } from '../components/States'
import { addDays, toDateInput } from '../utils/hours'

export function BookProviderPage() {
  const { providerId, serviceId } = useParams()
  const profileId = Number(providerId)
  const serviceNumericId = Number(serviceId)
  const navigate = useNavigate()
  const [provider, setProvider] = useState<PublicProvider | null>(null)
  const [service, setService] = useState<CatalogService | null>(null)
  const [requestedDate, setRequestedDate] = useState(() => toDateInput(addDays(new Date(), 1)))
  const [notes, setNotes] = useState('')
  const [hours, setHours] = useState<WorkingHour[] | null>(null)
  const [busy, setBusy] = useState<BusyInterval[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  const load = useCallback(async () => {
    if (Number.isNaN(profileId) || Number.isNaN(serviceNumericId)) {
      setError('This booking link is not valid.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      const [providerData, serviceData] = await Promise.all([
        getPublicProvider(profileId),
        getService(serviceNumericId),
      ])
      const offersService = providerData.services.some((item) => item.id === serviceData.id)
      if (!offersService) {
        setError('This provider does not offer that service.')
        setProvider(null)
        setService(null)
        return
      }

      setProvider(providerData)
      setService(serviceData)
      const from = toDateInput(new Date())
      const to = toDateInput(addDays(new Date(), 60))
      try {
        const availability = await getProviderAvailability(providerData.id, from, to)
        setHours(availability.workingHours)
        setBusy(availability.busy)
      } catch {
        setHours(null)
        setBusy([])
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load this booking.')
    } finally {
      setLoading(false)
    }
  }, [profileId, serviceNumericId])

  useEffect(() => {
    void load()
  }, [load])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting || !provider || !service) {
      return
    }

    const today = toDateInput(new Date())
    if (!requestedDate || requestedDate < today) {
      setFieldErrors({ requestedDate: ['Choose today or a later day.'] })
      setFormError(null)
      return
    }

    setSubmitting(true)
    setFormError(null)
    setFieldErrors({})
    try {
      const booking = await createBooking({
        providerProfileId: provider.id,
        serviceId: service.id,
        requestedDate,
        notes: notes.trim() || undefined,
      })
      navigate(`/account/bookings/${booking.id}`, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setFieldErrors(err.validationErrors)
        setFormError(err.message)
      } else {
        setFormError('Could not send the booking request.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <LoadingState label="Loading booking" />
  }

  if (error || !provider || !service) {
    return (
      <ErrorState
        title="Unable to start this booking"
        description={error ?? 'This booking link is not valid.'}
        onRetry={() => void load()}
      />
    )
  }

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow={service.categoryName}
        title={`Book ${service.name}`}
        description="Pick a day the provider works. They will call you to agree the time, then quote a price."
      />
      <section className="dashboard-panel">
        <div className="request-card-head">
          <ProviderAvatar
            providerId={provider.id}
            name={provider.fullName}
            hasPhoto={provider.hasPhoto}
            size="sm"
          />
          <div>
            <h2>{provider.fullName}</h2>
            <p className="muted">
              {provider.city} ·{' '}
              <Link to={`/providers/${provider.id}`}>View profile</Link>
            </p>
            <WorkingHoursSummary hours={hours ?? provider.workingHours ?? []} />
          </div>
        </div>
      </section>
      <form className="form dashboard-panel" onSubmit={(event) => void handleSubmit(event)}>
        {formError ? (
          <div className="alert" role="alert">
            {formError}
          </div>
        ) : null}
        <FormField
          label="Day"
          error={fieldError(fieldErrors, 'requestedDate')}
          hint="Closed days are unavailable. The exact time is agreed on the call."
        >
          <DayPicker
            value={requestedDate}
            hours={hours}
            busy={busy}
            onChange={setRequestedDate}
          />
        </FormField>
        <FormField
          label="Notes"
          error={fieldError(fieldErrors, 'notes')}
          hint="Optional. Describe the job so the provider can prepare."
        >
          <textarea
            rows={4}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            maxLength={1000}
          />
        </FormField>
        <Button type="submit" loading={submitting}>
          {submitting ? 'Sending request…' : 'Send booking request'}
        </Button>
      </form>
    </div>
  )
}

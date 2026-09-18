import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { FilePlus2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { getServices } from '../../api/catalog'
import { ApiError, fieldError } from '../../api/client'
import {
  createServiceRequest,
  getCustomerRequest,
  updateServiceRequest,
} from '../../api/requests'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import {
  fromDateTimeLocal,
  futureDateTimeLocal,
  toDateTimeLocal,
} from '../../utils/format'

export function CustomerRequestFormPage({ requestId }: { requestId?: number }) {
  const navigate = useNavigate()
  const isEdit = requestId !== undefined
  const [loading, setLoading] = useState(isEdit)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [servicesError, setServicesError] = useState<string | null>(null)
  const [services, setServices] = useState<{ id: number; name: string; categoryName: string }[]>(
    [],
  )
  const [serviceId, setServiceId] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [city, setCity] = useState('')
  const [preferredDate, setPreferredDate] = useState(futureDateTimeLocal(3))
  const [budgetMin, setBudgetMin] = useState('')
  const [budgetMax, setBudgetMax] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [success, setSuccess] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    setServicesError(null)
    try {
      const catalog = await getServices()
      setServices(catalog)
    } catch (err) {
      setServicesError(
        err instanceof ApiError ? err.message : 'Could not load services.',
      )
      setLoading(false)
      return
    }

    if (requestId === undefined) {
      setLoading(false)
      return
    }

    try {
      const detail = await getCustomerRequest(requestId)
      setServiceId(String(detail.serviceId))
      setTitle(detail.title)
      setDescription(detail.description)
      setCity(detail.city)
      setPreferredDate(toDateTimeLocal(detail.preferredDate))
      setBudgetMin(detail.budgetMin == null ? '' : String(detail.budgetMin))
      setBudgetMax(detail.budgetMax == null ? '' : String(detail.budgetMax))
    } catch (err) {
      setLoadError(err instanceof ApiError ? err.message : 'Could not load request.')
    } finally {
      setLoading(false)
    }
  }, [requestId])

  useEffect(() => {
    void load()
  }, [load])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    const payload = {
      title,
      description,
      preferredDate: fromDateTimeLocal(preferredDate),
      budgetMin: budgetMin === '' ? null : Number(budgetMin),
      budgetMax: budgetMax === '' ? null : Number(budgetMax),
    }

    try {
      const saved = isEdit
        ? await updateServiceRequest(requestId, payload)
        : await createServiceRequest({
            ...payload,
            serviceId: Number(serviceId),
            city,
          })
      setSuccess(isEdit ? 'Request updated.' : 'Request created.')
      navigate(`/customer/requests/${saved.id}`, {
        state: { notice: isEdit ? 'Request updated.' : 'Request created.' },
      })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setError('Could not save the request.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Customer}>
      <PageHeader
        eyebrow={isEdit ? 'Edit request' : 'New request'}
        title={isEdit ? 'Update your request' : 'Create a service request'}
        description="Describe the work, city, and budget so eligible providers can respond."
      />
      {loading ? <LoadingState label="Loading request form" count={2} /> : null}
      {loadError ? (
        <ErrorState
          title="Unable to load request"
          description={loadError}
          onRetry={() => void load()}
        />
      ) : null}
      {servicesError ? (
        <ErrorState
          title="Unable to load services"
          description={servicesError}
          onRetry={() => void load()}
        />
      ) : null}
      {!loading && !loadError && !servicesError ? (
        <form className="form workspace-form" onSubmit={(event) => void handleSubmit(event)}>
          {success ? (
            <div className="alert alert-success" role="status">
              {success}
            </div>
          ) : null}
          {error ? (
            <div className="alert" role="alert">
              {error}
            </div>
          ) : null}
          <section className="form-section">
            <h2 className="form-section-title">Service details</h2>
            <FormField label="Service" error={fieldError(fieldErrors, 'serviceId')}>
              <select
                value={serviceId}
                onChange={(event) => setServiceId(event.target.value)}
                required
                disabled={isEdit}
              >
                <option value="">Select a service</option>
                {services.map((service) => (
                  <option key={service.id} value={service.id}>
                    {service.categoryName} — {service.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Title" error={fieldError(fieldErrors, 'title')}>
              <input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                required
                maxLength={120}
              />
            </FormField>
            <FormField
              label="Description"
              error={fieldError(fieldErrors, 'description')}
            >
              <textarea
                rows={6}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                required
                maxLength={2000}
              />
            </FormField>
          </section>
          <section className="form-section">
            <h2 className="form-section-title">Schedule and budget</h2>
            <FormField label="City" error={fieldError(fieldErrors, 'city')}>
              <input
                value={city}
                onChange={(event) => setCity(event.target.value)}
                required
                disabled={isEdit}
              />
            </FormField>
            <FormField
              label="Preferred date and time"
              error={fieldError(fieldErrors, 'preferredDate')}
            >
              <input
                type="datetime-local"
                value={preferredDate}
                onChange={(event) => setPreferredDate(event.target.value)}
                required
              />
            </FormField>
            <div className="form-split">
              <FormField
                label="Minimum budget (optional)"
                error={fieldError(fieldErrors, 'budgetMin')}
              >
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={budgetMin}
                  onChange={(event) => setBudgetMin(event.target.value)}
                />
              </FormField>
              <FormField
                label="Maximum budget (optional)"
                error={fieldError(fieldErrors, 'budgetMax')}
              >
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={budgetMax}
                  onChange={(event) => setBudgetMax(event.target.value)}
                />
              </FormField>
            </div>
          </section>
          <Button type="submit" icon={FilePlus2} loading={submitting}>
            {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create request'}
          </Button>
        </form>
      ) : null}
    </WorkspaceLayout>
  )
}

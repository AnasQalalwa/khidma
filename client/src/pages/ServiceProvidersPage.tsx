import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getService, getServiceProviders, type ServiceProvider } from '../api/catalog'
import { ApiError } from '../api/client'
import type { CatalogService } from '../api/catalog'
import { useAuth } from '../auth/useAuth'
import { Button } from '../components/Button'
import { PageHeader } from '../components/PageHeader'
import { ProviderAvatar } from '../components/ProviderAvatar'
import { StatusBadge } from '../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PALESTINIAN_CITIES } from '../data/cities'
import { WorkingHoursSummary } from '../components/schedule/WorkingHoursSummary'
import { formatRating } from '../utils/format'

export function ServiceProvidersPage() {
  const { id } = useParams()
  const serviceId = Number(id)
  const { user, loading: authLoading } = useAuth()
  const [service, setService] = useState<CatalogService | null>(null)
  const [providers, setProviders] = useState<ServiceProvider[]>([])
  const [city, setCity] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (authLoading || city !== null) {
      return
    }

    setCity(user?.role === 'Customer' && user.city ? user.city : '')
  }, [authLoading, city, user])

  const load = useCallback(async () => {
    if (Number.isNaN(serviceId) || city === null) {
      if (Number.isNaN(serviceId)) {
        setError('Service not found.')
        setLoading(false)
      }
      return
    }

    setLoading(true)
    setError(null)
    try {
      const [serviceData, providerData] = await Promise.all([
        getService(serviceId),
        getServiceProviders(serviceId, city || undefined),
      ])
      setService(serviceData)
      setProviders(providerData)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load providers.')
    } finally {
      setLoading(false)
    }
  }, [city, serviceId])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div className="page-stack">
      {loading ? <LoadingState label="Loading providers" /> : null}
      {error ? (
        <ErrorState title="Unable to load providers" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && service ? (
        <>
          <PageHeader
            eyebrow={service.categoryName}
            title={service.name}
            description="Choose a verified provider, then pick a day. You agree the time on a call."
          />
          <div className="filter-bar">
            <label htmlFor="provider-city">
              City
              <select
                id="provider-city"
                value={city ?? ''}
                onChange={(event) => setCity(event.target.value)}
              >
                <option value="">All cities</option>
                {PALESTINIAN_CITIES.map((option) => (
                  <option key={option.name} value={option.name}>
                    {option.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
          {providers.length === 0 ? (
            <EmptyState
              title="No providers yet"
              description="No approved providers offer this service in the selected city."
            />
          ) : (
            <div className="card-grid">
              {providers.map((provider) => (
                <article className="request-card" key={provider.id}>
                  <div className="request-card-head">
                    <ProviderAvatar
                      providerId={provider.id}
                      name={provider.fullName}
                      hasPhoto={provider.hasPhoto}
                      size="sm"
                    />
                    <div>
                      <h3>{provider.fullName}</h3>
                      <p className="muted">{provider.city}</p>
                    </div>
                    {provider.isVerified ? <StatusBadge status="Approved" /> : null}
                  </div>
                  <p className="muted">
                    {formatRating(provider.averageRating, provider.reviewCount)} ·{' '}
                    {provider.yearsOfExperience} years · {provider.completedJobs} completed
                  </p>
                  <WorkingHoursSummary hours={provider.workingHours ?? []} />
                  {provider.bio ? <p>{provider.bio}</p> : null}
                  <div className="dashboard-actions">
                    <Button to={`/providers/${provider.id}`} variant="secondary" size="sm">
                      Profile
                    </Button>
                    <Button to={`/book/${provider.id}/${service.id}`} size="sm">
                      Book
                    </Button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </>
      ) : null}
    </div>
  )
}

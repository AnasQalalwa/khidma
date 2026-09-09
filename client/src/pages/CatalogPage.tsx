import { useCallback, useEffect, useMemo, useState } from 'react'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { Button } from '../components/Button'
import {
  EmptyState,
  LoadingState,
  ServiceCard,
} from '../components/CategoryCard'
import { PageHeader } from '../components/PageHeader'

const CATALOG_ERROR = "We couldn't load services right now. Please try again."

export function CatalogPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [categoryId, setCategoryId] = useState<number | 'all'>('all')
  const [reloadToken, setReloadToken] = useState(0)

  const retryCatalog = useCallback(() => {
    setReloadToken((token) => token + 1)
  }, [])

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError(null)
      try {
        const [categoryData, serviceData] = await Promise.all([
          getCategories(),
          getServices(),
        ])
        if (!cancelled) {
          setCategories(categoryData)
          setServices(serviceData)
        }
      } catch {
        if (!cancelled) {
          setError(CATALOG_ERROR)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [reloadToken])

  const visibleServices = useMemo(() => {
    const needle = query.trim().toLowerCase()
    return services.filter((service) => {
      const matchesCategory =
        categoryId === 'all' || service.categoryId === categoryId
      const matchesQuery =
        needle.length === 0 ||
        service.name.toLowerCase().includes(needle) ||
        service.categoryName.toLowerCase().includes(needle)
      return matchesCategory && matchesQuery
    })
  }, [categoryId, query, services])

  return (
    <section>
      <PageHeader
        eyebrow="Marketplace"
        title="Service catalog"
        description="Browse live categories and services from the Khidma API."
      />

      {loading ? <LoadingState label="Loading catalog" /> : null}

      {!loading && error ? (
        <div className="error-state status-block" role="alert">
          <h2>Services unavailable</h2>
          <p className="muted">{error}</p>
          <div className="status-actions">
            <Button variant="secondary" onClick={retryCatalog}>
              Try again
            </Button>
          </div>
        </div>
      ) : null}

      {!loading && !error && categories.length === 0 ? (
        <EmptyState
          title="No services yet"
          description="No categories have been published yet."
        />
      ) : null}

      {!loading && !error && categories.length > 0 ? (
        <>
          <div className="toolbar">
            <input
              className="search-input"
              type="search"
              placeholder="Search services"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              aria-label="Search services"
            />
            <button
              type="button"
              className="filter-chip"
              aria-pressed={categoryId === 'all'}
              onClick={() => setCategoryId('all')}
            >
              All
            </button>
            {categories.map((category) => (
              <button
                key={category.id}
                type="button"
                className="filter-chip"
                aria-pressed={categoryId === category.id}
                onClick={() => setCategoryId(category.id)}
              >
                {category.name}
              </button>
            ))}
          </div>

          {visibleServices.length === 0 ? (
            <EmptyState
              title="No matching services"
              description="Try another category or search term."
            />
          ) : (
            <div className="service-grid">
              {visibleServices.map((service) => (
                <ServiceCard
                  key={service.id}
                  name={service.name}
                  categoryName={service.categoryName}
                />
              ))}
            </div>
          )}
        </>
      ) : null}
    </section>
  )
}

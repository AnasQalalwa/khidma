import { useCallback, useEffect, useMemo, useState } from 'react'
import { Search, X } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { Icon } from '../components/icons'
import { PageHeader } from '../components/PageHeader'
import { ServiceCard } from '../components/ServiceCard'
import { EmptyState, ErrorState, LoadingState, SearchEmptyState } from '../components/States'

const CATALOG_ERROR = "We couldn't load services right now. Please try again."

export function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [reloadToken, setReloadToken] = useState(0)

  const categoryParam = searchParams.get('category')
  const categoryId = categoryParam ? Number(categoryParam) : 'all'

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

  const selectedCategoryId = useMemo(() => {
    if (categoryId === 'all' || Number.isNaN(categoryId)) {
      return 'all' as const
    }

    return categories.some((category) => category.id === categoryId)
      ? categoryId
      : 'all'
  }, [categories, categoryId])

  const visibleServices = useMemo(() => {
    const needle = query.trim().toLowerCase()
    return services.filter((service) => {
      const matchesCategory =
        selectedCategoryId === 'all' || service.categoryId === selectedCategoryId
      const matchesQuery =
        needle.length === 0 ||
        service.name.toLowerCase().includes(needle) ||
        service.categoryName.toLowerCase().includes(needle)
      return matchesCategory && matchesQuery
    })
  }, [query, selectedCategoryId, services])

  function selectCategory(next: number | 'all') {
    if (next === 'all') {
      setSearchParams({})
      return
    }

    setSearchParams({ category: String(next) })
  }

  return (
    <section>
      <PageHeader
        eyebrow="Marketplace"
        title="Find the right service"
        description="Browse live categories and services from the Khidma catalog. Booking requests will arrive in a later phase."
      />

      {loading ? <LoadingState label="Loading catalog" count={6} /> : null}

      {!loading && error ? (
        <ErrorState
          title="Services unavailable"
          description={error}
          onRetry={retryCatalog}
        />
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
            <div className="search-field">
              <Icon icon={Search} size={16} className="field-icon" />
              <input
                className="search-input"
                type="search"
                placeholder="Search services"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                aria-label="Search services"
              />
              {query ? (
                <button
                  type="button"
                  className="search-clear"
                  aria-label="Clear search"
                  onClick={() => setQuery('')}
                >
                  <Icon icon={X} size={16} />
                </button>
              ) : null}
            </div>
            <button
              type="button"
              className="filter-chip"
              aria-pressed={selectedCategoryId === 'all'}
              onClick={() => selectCategory('all')}
            >
              All
            </button>
            {categories.map((category) => (
              <button
                key={category.id}
                type="button"
                className="filter-chip"
                aria-pressed={selectedCategoryId === category.id}
                onClick={() => selectCategory(category.id)}
              >
                {category.name}
              </button>
            ))}
          </div>

          <p className="catalog-meta">
            {visibleServices.length}{' '}
            {visibleServices.length === 1 ? 'service' : 'services'} shown
          </p>

          {visibleServices.length === 0 ? (
            <SearchEmptyState />
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

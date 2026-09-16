import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import {
  AlertCircle,
  Lightbulb,
  RotateCcw,
  Search,
  SearchX,
  Store,
} from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { Button } from '../components/Button'
import { Icon } from '../components/icons'
import { ServiceCard } from '../components/ServiceCard'
import { getCategoryIcon } from '../utils/catalogVisuals'
import { catalogHeroImage } from '../utils/serviceVisuals'

const CATALOG_ERROR = "We couldn't load the catalog."

type SortOption = 'name-asc' | 'name-desc' | 'category'

function CatalogSkeletons() {
  return (
    <div className="catalog-grid" aria-busy="true" aria-live="polite">
      <p className="sr-only">Loading catalog</p>
      {Array.from({ length: 8 }, (_, index) => (
        <div className="catalog-card catalog-card-skeleton" key={index}>
          <div className="catalog-card-media skeleton" />
          <div className="catalog-card-body">
            <span className="skeleton catalog-skel-badge" />
            <span className="skeleton catalog-skel-title" />
            <span className="skeleton catalog-skel-line" />
            <span className="skeleton catalog-skel-line catalog-skel-line-short" />
          </div>
        </div>
      ))}
    </div>
  )
}

export function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [sort, setSort] = useState<SortOption>('name-asc')
  const [reloadToken, setReloadToken] = useState(0)
  const searchRef = useRef<HTMLInputElement>(null)
  const resultsRef = useRef<HTMLDivElement>(null)

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

  const categoryCounts = useMemo(() => {
    const counts = new Map<number, number>()
    for (const service of services) {
      counts.set(service.categoryId, (counts.get(service.categoryId) ?? 0) + 1)
    }
    return counts
  }, [services])

  const visibleServices = useMemo(() => {
    const needle = query.trim().toLowerCase()
    const filtered = services.filter((service) => {
      const matchesCategory =
        selectedCategoryId === 'all' || service.categoryId === selectedCategoryId
      const matchesQuery =
        needle.length === 0 ||
        service.name.toLowerCase().includes(needle) ||
        service.categoryName.toLowerCase().includes(needle)
      return matchesCategory && matchesQuery
    })

    return filtered.sort((left, right) => {
      if (sort === 'category') {
        const byCategory = left.categoryName.localeCompare(right.categoryName)
        return byCategory !== 0 ? byCategory : left.name.localeCompare(right.name)
      }

      const byName = left.name.localeCompare(right.name)
      return sort === 'name-desc' ? -byName : byName
    })
  }, [query, selectedCategoryId, services, sort])

  function selectCategory(next: number | 'all') {
    if (next === 'all') {
      setSearchParams({})
      return
    }

    setSearchParams({ category: String(next) })
  }

  function clearFilters() {
    setQuery('')
    setSort('name-asc')
    selectCategory('all')
  }

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    resultsRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  function scrollToCatalog() {
    searchRef.current?.focus()
    resultsRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  const showCatalogBody = !loading && !error && categories.length > 0
  const showEmptyCatalog = !loading && !error && categories.length === 0

  return (
    <div className="catalog-page">
      <section className="catalog-hero">
        <div className="container catalog-hero-inner">
          <div className="catalog-hero-copy">
            <span className="eyebrow">Services marketplace</span>
            <h1>
              Find the right <span>service</span>
            </h1>
            <p>
              Browse live categories and services from the Khidma catalog.
              Booking requests will arrive in a later phase.
            </p>
          </div>
          <div className="catalog-hero-visual">
            <span className="catalog-hero-blob catalog-hero-blob-a" aria-hidden="true" />
            <span className="catalog-hero-blob catalog-hero-blob-b" aria-hidden="true" />
            <article className="catalog-hero-card">
              <span className="catalog-hero-card-icon" aria-hidden="true">
                <Icon icon={Store} size={16} />
              </span>
              <div>
                <strong>
                  Local people.
                  <br />
                  Real solutions.
                </strong>
                <p>Find trusted professionals in your community.</p>
              </div>
            </article>
            <img
              className="catalog-hero-photo"
              src={catalogHeroImage}
              alt="Bright living room representing local home services"
            />
            <p className="catalog-hero-script" aria-hidden="true">
              Better Services
              <br />
              Brighter Days
            </p>
          </div>
        </div>
      </section>

      <div className="container catalog-shell">
        <form className="catalog-search" role="search" onSubmit={handleSearchSubmit}>
          <label className="sr-only" htmlFor="catalog-search-input">
            Search services
          </label>
          <Icon icon={Search} size={20} className="catalog-search-icon" />
          <input
            id="catalog-search-input"
            ref={searchRef}
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search services (e.g. cleaning, plumbing, tutoring...)"
          />
          <Button type="submit">Search</Button>
        </form>

        {loading ? <CatalogSkeletons /> : null}

        {!loading && error ? (
          <div className="catalog-state" role="alert">
            <span className="catalog-state-icon" aria-hidden="true">
              <Icon icon={AlertCircle} size={24} />
            </span>
            <h2>We couldn't load the catalog.</h2>
            <p>Please try again.</p>
            <Button variant="secondary" icon={RotateCcw} onClick={retryCatalog}>
              Retry
            </Button>
          </div>
        ) : null}

        {showEmptyCatalog ? (
          <div className="catalog-state">
            <span className="catalog-state-icon" aria-hidden="true">
              <Icon icon={SearchX} size={24} />
            </span>
            <h2>No services found</h2>
            <p>No categories have been published yet.</p>
          </div>
        ) : null}

        {showCatalogBody ? (
          <div className="catalog-layout">
            <aside className="catalog-sidebar">
              <section className="catalog-side-card">
                <h2>Categories</h2>
                <ul>
                  <li>
                    <button
                      type="button"
                      className="catalog-side-row"
                      aria-pressed={selectedCategoryId === 'all'}
                      onClick={() => selectCategory('all')}
                    >
                      <span>
                        <Icon icon={getCategoryIcon('all')} size={16} />
                        All Categories
                      </span>
                      <strong>{services.length}</strong>
                    </button>
                  </li>
                  {categories.map((category) => (
                    <li key={category.id}>
                      <button
                        type="button"
                        className="catalog-side-row"
                        aria-pressed={selectedCategoryId === category.id}
                        onClick={() => selectCategory(category.id)}
                      >
                        <span>
                          <Icon icon={getCategoryIcon(category.name)} size={16} />
                          {category.name}
                        </span>
                        <strong>{categoryCounts.get(category.id) ?? 0}</strong>
                      </button>
                    </li>
                  ))}
                </ul>
              </section>

              <section className="catalog-side-card catalog-side-note">
                <span className="catalog-note-icon" aria-hidden="true">
                  <Icon icon={Lightbulb} size={16} />
                </span>
                <h2>Don't see what you need?</h2>
                <p>More services will be available in the next phase.</p>
              </section>
            </aside>

            <div className="catalog-main" ref={resultsRef} id="catalog-results">
              <div className="catalog-pills" role="toolbar" aria-label="Filter by category">
                <button
                  type="button"
                  className="catalog-pill"
                  aria-pressed={selectedCategoryId === 'all'}
                  onClick={() => selectCategory('all')}
                >
                  All
                </button>
                {categories.map((category) => (
                  <button
                    type="button"
                    className="catalog-pill"
                    key={category.id}
                    aria-pressed={selectedCategoryId === category.id}
                    onClick={() => selectCategory(category.id)}
                  >
                    {category.name}
                  </button>
                ))}
              </div>

              <div className="catalog-toolbar">
                  <p>
                    {visibleServices.length}{' '}
                    {visibleServices.length === 1 ? 'service' : 'services'} shown
                  </p>
                  <label className="catalog-sort" htmlFor="catalog-sort">
                    <span>Sort by</span>
                    <select
                      id="catalog-sort"
                      value={sort}
                      onChange={(event) => setSort(event.target.value as SortOption)}
                    >
                      <option value="name-asc">Name (A-Z)</option>
                      <option value="name-desc">Name (Z-A)</option>
                      <option value="category">Category</option>
                    </select>
                  </label>
                </div>

                {visibleServices.length === 0 ? (
                  <div className="catalog-state catalog-state-inline">
                    <span className="catalog-state-icon" aria-hidden="true">
                      <Icon icon={SearchX} size={24} />
                    </span>
                    <h2>No services found</h2>
                    <p>Try another search or category.</p>
                    <Button variant="secondary" onClick={clearFilters}>
                      Clear filters
                    </Button>
                  </div>
                ) : (
                  <div className="catalog-grid">
                    {visibleServices.map((service) => (
                      <ServiceCard
                        key={service.id}
                        name={service.name}
                        categoryName={service.categoryName}
                      />
                    ))}
                  </div>
                )}
              </div>
            </div>
        ) : null}

        <section className="catalog-cta">
          <span className="catalog-cta-leaf catalog-cta-leaf-a" aria-hidden="true" />
          <span className="catalog-cta-leaf catalog-cta-leaf-b" aria-hidden="true" />
          <div className="catalog-cta-copy">
            <span className="eyebrow">Need a service that isn't listed?</span>
            <h2>Let us know what you need.</h2>
            <p>More services and providers will be available in the next phase.</p>
          </div>
          <Button variant="light" icon={Search} onClick={scrollToCatalog}>
            Explore services
          </Button>
        </section>
      </div>
    </div>
  )
}

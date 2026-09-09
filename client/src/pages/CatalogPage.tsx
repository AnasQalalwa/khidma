import { useEffect, useMemo, useState } from 'react'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { ApiError } from '../api/client'

export function CatalogPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Could not load catalog.')
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
  }, [])

  const grouped = useMemo(() => {
    return categories.map((category) => ({
      category,
      services: services.filter((service) => service.categoryId === category.id),
    }))
  }, [categories, services])

  if (loading) {
    return <div className="status-block">Loading catalog…</div>
  }

  if (error) {
    return <div className="status-block alert">{error}</div>
  }

  if (categories.length === 0) {
    return <div className="status-block">No categories have been published yet.</div>
  }

  return (
    <section>
      <h1 className="page-title">Service catalog</h1>
      <p className="lead">Live categories and services from the Khidma API.</p>
      <div className="catalog-grid">
        {grouped.map(({ category, services: categoryServices }) => (
          <article key={category.id} className="catalog-group">
            <h2>{category.name}</h2>
            {categoryServices.length === 0 ? (
              <p className="muted">No services in this category yet.</p>
            ) : (
              <ul>
                {categoryServices.map((service) => (
                  <li key={service.id}>{service.name}</li>
                ))}
              </ul>
            )}
          </article>
        ))}
      </div>
    </section>
  )
}

import { useCallback, useEffect, useState } from 'react'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { useAuth } from '../auth/AuthContext'
import { dashboardPath } from '../auth/roles'
import { Button } from '../components/Button'
import { CategoryCard, EmptyState, LoadingState } from '../components/CategoryCard'

const CATALOG_ERROR = "We couldn't load services right now. Please try again."

export function HomePage() {
  const { authenticated, user } = useAuth()
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
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
          setCategories([])
          setServices([])
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

  return (
    <>
      <section className="hero">
        <div className="container hero-grid">
          <div>
            <span className="eyebrow">Service marketplace</span>
            <h1>Trusted services, right when you need them.</h1>
            <p className="lead hero-copy">
              Find verified local professionals, compare offers, and book the
              right service with confidence.
            </p>
            <div className="hero-actions">
              <Button to="/catalog">Browse Services</Button>
              {authenticated && user ? (
                <Button variant="secondary" to={dashboardPath(user.role)}>
                  Go to dashboard
                </Button>
              ) : (
                <Button variant="secondary" to="/register">
                  Get Started
                </Button>
              )}
            </div>
          </div>
          <div className="hero-visual" aria-hidden="true">
            <div className="hero-panel">
              <div className="mini-card">
                <strong>Request received</strong>
                <span>Customers describe the work they need.</span>
              </div>
              <div className="mini-card">
                <strong>Offers compared</strong>
                <span>Eligible providers send clear quotes.</span>
              </div>
              <div className="mini-card">
                <strong>Booking confirmed</strong>
                <span>One accepted offer becomes a booking.</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="section-head">
            <span className="eyebrow">Why Khidma</span>
            <h2>A clearer path from request to completed work</h2>
          </div>
          <div className="trust-grid">
            <article className="trust-card">
              <div className="icon-wrap" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                  <path
                    d="M12 3 5 6v6c0 4.5 3.1 7.8 7 9 3.9-1.2 7-4.5 7-9V6l-7-3Z"
                    stroke="currentColor"
                    strokeWidth="1.8"
                  />
                </svg>
              </div>
              <h3>Verified providers</h3>
              <p className="muted">
                Work with approved professionals who serve your city and
                service category.
              </p>
            </article>
            <article className="trust-card">
              <div className="icon-wrap" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                  <path
                    d="M7 7h13M7 12h13M7 17h13M4 7h.01M4 12h.01M4 17h.01"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                  />
                </svg>
              </div>
              <h3>Compare offers</h3>
              <p className="muted">
                Review price, message, and timing before accepting exactly one
                offer.
              </p>
            </article>
            <article className="trust-card">
              <div className="icon-wrap" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                  <path
                    d="M4 12h4l2.5-4 3 8 2.5-4H20"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </div>
              <h3>Secure booking workflow</h3>
              <p className="muted">
                Cookie authentication, role-based access, and a controlled
                booking lifecycle.
              </p>
            </article>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="section-head">
            <span className="eyebrow">How it works</span>
            <h2>Four steps from request to review</h2>
          </div>
          <div className="step-grid">
            <article className="step-card">
              <p className="step-index">Step 1</p>
              <h3>Request a service</h3>
              <p className="muted">Describe the job, city, and budget.</p>
            </article>
            <article className="step-card">
              <p className="step-index">Step 2</p>
              <h3>Receive offers</h3>
              <p className="muted">Eligible providers respond with quotes.</p>
            </article>
            <article className="step-card">
              <p className="step-index">Step 3</p>
              <h3>Choose a provider</h3>
              <p className="muted">Accept one offer to create a booking.</p>
            </article>
            <article className="step-card">
              <p className="step-index">Step 4</p>
              <h3>Complete and review</h3>
              <p className="muted">Track the job, then leave one review.</p>
            </article>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="section-head">
            <span className="eyebrow">Catalog</span>
            <h2>Popular service categories</h2>
            <p className="muted">Live from the Khidma catalog API.</p>
          </div>
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
              description="Categories will appear here once the catalog is available."
            />
          ) : null}

          {!loading && !error && categories.length > 0 ? (
            <div className="category-grid">
              {categories.map((category) => (
                <CategoryCard
                  key={category.id}
                  name={category.name}
                  serviceCount={
                    services.filter((service) => service.categoryId === category.id).length
                  }
                  to="/catalog"
                />
              ))}
            </div>
          ) : null}
        </div>
      </section>

      <div className="container">
        <section className="cta-band">
          <div>
            <h2>Ready to book with confidence?</h2>
            <p>
              {authenticated
                ? 'Open your dashboard or browse the live service catalog.'
                : 'Create an account or browse the live service catalog.'}
            </p>
          </div>
          <div className="hero-actions">
            <Button to="/catalog">Browse Services</Button>
            {authenticated ? (
              <Button variant="secondary" to={dashboardPath(user?.role ?? 'Customer')}>
                Open dashboard
              </Button>
            ) : (
              <Button variant="secondary" to="/register">
                Get Started
              </Button>
            )}
          </div>
        </section>
      </div>
    </>
  )
}

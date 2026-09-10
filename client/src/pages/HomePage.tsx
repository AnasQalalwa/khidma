import { useCallback, useEffect, useState } from 'react'
import {
  CalendarCheck,
  CheckCircle2,
  FileText,
  Inbox,
  Lock,
  Search,
  ShieldCheck,
  Sparkles,
  Star,
  UserCheck,
} from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCategories, getServices, type CatalogService, type Category } from '../api/catalog'
import { Button } from '../components/Button'
import { CategoryCard } from '../components/CategoryCard'
import { Icon, IconTile } from '../components/icons'
import { SectionHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../components/States'

const CATALOG_ERROR = "We couldn't load services right now. Please try again."

export function HomePage() {
  const location = useLocation()
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  const retryCatalog = useCallback(() => {
    setReloadToken((token) => token + 1)
  }, [])

  useEffect(() => {
    if (location.hash !== '#how-it-works') {
      return
    }

    const node = document.getElementById('how-it-works')
    node?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [location.hash])

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
            <span className="eyebrow">Local people. Real solutions.</span>
            <h1>Trusted services, right when you need them.</h1>
            <p className="lead hero-copy">
              Find verified local professionals, compare offers, and book with
              confidence. Khidma makes it easy to find local help without
              guesswork.
            </p>
            <div className="hero-actions">
              <Button to="/catalog" icon={Search}>
                Find a Service
              </Button>
              <Button variant="secondary" to="/#how-it-works">
                How It Works
              </Button>
            </div>
          </div>
          <div className="hero-visual" aria-hidden="true">
            <span className="hero-blob hero-blob-one" />
            <span className="hero-blob hero-blob-two" />
            <div className="hero-panel">
              <div className="mini-card">
                <span className="mini-icon">
                  <Icon icon={FileText} size={16} />
                </span>
                <div>
                  <strong>Request a service</strong>
                  <span>Describe the work you need in your city.</span>
                </div>
              </div>
              <div className="mini-card">
                <span className="mini-icon">
                  <Icon icon={Inbox} size={16} />
                </span>
                <div>
                  <strong>Compare offers</strong>
                  <span>Eligible providers respond with clear quotes.</span>
                </div>
              </div>
              <div className="mini-card">
                <span className="mini-icon">
                  <Icon icon={CalendarCheck} size={16} />
                </span>
                <div>
                  <strong>Book with confidence</strong>
                  <span>Accept one offer and follow the job through.</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div className="container">
        <div className="trust-strip">
          <article className="trust-item">
            <IconTile icon={ShieldCheck} accent="home" />
            <div>
              <strong>Verified providers</strong>
              <span>Work with approved local professionals.</span>
            </div>
          </article>
          <article className="trust-item">
            <IconTile icon={Sparkles} accent="cleaning" />
            <div>
              <strong>Fast booking</strong>
              <span>Request help and review offers in one place.</span>
            </div>
          </article>
          <article className="trust-item">
            <IconTile icon={Lock} accent="technology" />
            <div>
              <strong>Secure workflow</strong>
              <span>Protected accounts and a controlled booking path.</span>
            </div>
          </article>
        </div>
      </div>

      <section className="section">
        <div className="container">
          <SectionHeader
            eyebrow="Popular categories"
            title="Find help across everyday services"
            description="Live categories from the Khidma catalog."
          />
          {loading ? <LoadingState label="Loading catalog" /> : null}
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
                    services.filter((service) => service.categoryId === category.id)
                      .length
                  }
                  to={`/catalog?category=${category.id}`}
                />
              ))}
            </div>
          ) : null}
        </div>
      </section>

      <section className="section" id="how-it-works">
        <div className="container">
          <SectionHeader
            eyebrow="How Khidma works"
            title="Four steps from request to review"
            description="A clear path from describing the job to leaving one review."
          />
          <div className="step-grid">
            <article className="step-card">
              <IconTile icon={FileText} accent="home" />
              <p className="step-index">Step 1</p>
              <h3>Request a service</h3>
              <p className="muted">Describe the job, city, and budget.</p>
            </article>
            <article className="step-card">
              <IconTile icon={Inbox} accent="technology" />
              <p className="step-index">Step 2</p>
              <h3>Receive offers</h3>
              <p className="muted">Eligible providers respond with quotes.</p>
            </article>
            <article className="step-card">
              <IconTile icon={UserCheck} accent="education" />
              <p className="step-index">Step 3</p>
              <h3>Choose a provider</h3>
              <p className="muted">Accept one offer to create a booking.</p>
            </article>
            <article className="step-card">
              <IconTile icon={Star} accent="electrical" />
              <p className="step-index">Step 4</p>
              <h3>Complete and review</h3>
              <p className="muted">Track the job, then leave one review.</p>
            </article>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <SectionHeader
            eyebrow="Why Khidma"
            title="A marketplace built for trust"
            description="Designed around a controlled request, offer, booking, and review flow."
          />
          <div className="why-grid">
            <article className="why-card">
              <IconTile icon={ShieldCheck} accent="home" />
              <h3>Verified providers</h3>
              <p className="muted">
                Work with approved professionals who serve your city and
                service category.
              </p>
            </article>
            <article className="why-card">
              <IconTile icon={Inbox} accent="technology" />
              <h3>Compare offers</h3>
              <p className="muted">
                Review price, message, and timing before accepting exactly one
                offer.
              </p>
            </article>
            <article className="why-card">
              <IconTile icon={CalendarCheck} accent="cleaning" />
              <h3>Easy booking management</h3>
              <p className="muted">
                Follow work from scheduled to completed in one place.
              </p>
            </article>
            <article className="why-card">
              <IconTile icon={CheckCircle2} accent="education" />
              <h3>Safe controlled workflow</h3>
              <p className="muted">
                Cookie authentication, role-based access, and a defined booking
                lifecycle.
              </p>
            </article>
          </div>
        </div>
      </section>

      <div className="container">
        <section className="cta-band">
          <div>
            <h2>Find trusted professionals near you.</h2>
            <p>Browse live services and start with a clear, secure workflow.</p>
          </div>
          <div className="hero-actions">
            <Button to="/catalog" icon={Search}>
              Find a Service
            </Button>
          </div>
        </section>
      </div>
    </>
  )
}

import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  ArrowRight,
  BadgeCheck,
  Briefcase,
  CalendarCheck,
  ChevronRight,
  CirclePlay,
  FilePenLine,
  Lock,
  MessagesSquare,
  Scale,
  ShieldCheck,
  Sparkles,
  Star,
  UserCheck,
  Users,
  Workflow,
  Zap,
} from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  categoryImageUrl,
  getCategories,
  getServices,
  type CatalogService,
  type Category,
} from '../api/catalog'
import heroProfessional from '../assets/hero-professional.webp'
import { Button } from '../components/Button'
import { CategoryCard } from '../components/CategoryCard'
import { Icon, IconTile } from '../components/icons'
import { EmptyState, ErrorState, LoadingState } from '../components/States'

const CATALOG_ERROR = "We couldn't load services right now. Please try again."

function HomeSection({
  id,
  title,
  subtitle,
  action,
  children,
}: {
  id?: string
  title: string
  subtitle?: string
  action?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="home-section" id={id}>
      <div className="container">
        <div className={action ? 'home-section-head has-action' : 'home-section-head'}>
          <div className="home-section-copy">
            <h2>{title}</h2>
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          {action}
        </div>
        {children}
      </div>
    </section>
  )
}

export function HomePage() {
  const location = useLocation()
  const navigate = useNavigate()
  const [homeQuery, setHomeQuery] = useState('')
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
    <div className="home-page">
      <section className="hero">
        <div className="container">
          <div className="hero-grid">
            <div className="hero-copy-block">
              <span className="eyebrow">Local people. Real solutions.</span>
              <h1>
                <span className="hero-title-navy">Trusted services,</span>
                <span className="hero-title-teal">
                  right when you
                  <span className="hero-title-rest"> need them.</span>
                </span>
              </h1>
              <p className="lead hero-copy">
                Find verified local professionals, pick a day, and send a booking
                request. Khidma makes it simple to get things done — at home,
                at work, and in your community.
              </p>
              <form
                className="hero-search"
                role="search"
                onSubmit={(event) => {
                  event.preventDefault()
                  const q = homeQuery.trim()
                  navigate(q ? `/catalog?q=${encodeURIComponent(q)}` : '/catalog')
                }}
              >
                <label className="sr-only" htmlFor="home-search">
                  Search services
                </label>
                <input
                  id="home-search"
                  type="search"
                  placeholder="Try plumbing, cleaning, tutoring"
                  value={homeQuery}
                  onChange={(event) => setHomeQuery(event.target.value)}
                />
                <Button type="submit">Search</Button>
              </form>
              <div className="hero-actions">
                <Button to="/catalog" iconRight={ArrowRight}>
                  Find a Service
                </Button>
                <Button variant="secondary" to="/#how-it-works" icon={CirclePlay}>
                  How It Works
                </Button>
              </div>
            </div>

            <div className="hero-visual">
              <span className="hero-shape hero-shape-one" aria-hidden="true" />
              <span className="hero-shape hero-shape-two" aria-hidden="true" />
              <span className="hero-shape hero-shape-three" aria-hidden="true" />
              <img
                className="hero-photo"
                src={heroProfessional}
                alt="Khidma service professional"
                width={900}
                height={1124}
              />
              <article className="hero-float-card">
                <span className="hero-float-icon">
                  <Icon icon={Sparkles} size={16} />
                </span>
                <h2>
                  A cleaner home,
                  <br />
                  happier days
                </h2>
                <p>Trusted local service</p>
                <Button to="/catalog" size="sm">
                  View Services
                </Button>
              </article>
              <span className="hero-verified">
                <Icon icon={BadgeCheck} size={16} />
                Verified Professional
              </span>
            </div>

            <div className="trust-strip">
              <article className="trust-item">
                <span className="trust-icon">
                  <Icon icon={ShieldCheck} size={20} />
                </span>
                <div>
                  <strong>Verified providers</strong>
                  <span>Approved local professionals</span>
                </div>
              </article>
              <article className="trust-item">
                <span className="trust-icon">
                  <Icon icon={Zap} size={20} />
                </span>
                <div>
                  <strong>Fast booking</strong>
                  <span>Book a verified provider directly</span>
                </div>
              </article>
              <article className="trust-item">
                <span className="trust-icon">
                  <Icon icon={Lock} size={20} />
                </span>
                <div>
                  <strong>Secure workflow</strong>
                  <span>Protected accounts and controlled booking flow</span>
                </div>
              </article>
            </div>
          </div>
        </div>
      </section>

      <HomeSection
        title="Popular Categories"
        subtitle="Browse our most in-demand services and find the right professional for your needs."
        action={
          <Link className="section-link" to="/catalog">
            View all categories
            <Icon icon={ArrowRight} size={16} />
          </Link>
        }
      >
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
                description={category.description}
                imageUrl={category.hasImage ? categoryImageUrl(category.id) : null}
                serviceCount={
                  services.filter((service) => service.categoryId === category.id)
                    .length
                }
                to={`/catalog?category=${category.id}`}
              />
            ))}
          </div>
        ) : null}
      </HomeSection>

      <HomeSection
        id="how-it-works"
        title="How Khidma Works"
        subtitle="From catalog to review — getting help is simple and secure."
      >
        <div className="step-grid">
          <article className="step-card">
            <div className="step-card-top">
              <span className="step-index">1</span>
              <IconTile icon={FilePenLine} accent="home" />
            </div>
            <h3>Choose a service</h3>
            <p className="muted">Browse the catalog and open a service.</p>
          </article>
          <span className="step-chevron" aria-hidden="true">
            <Icon icon={ChevronRight} size={20} />
          </span>
          <article className="step-card">
            <div className="step-card-top">
              <span className="step-index">2</span>
              <IconTile icon={MessagesSquare} accent="technology" />
            </div>
            <h3>Pick a provider</h3>
            <p className="muted">See who offers it in your city.</p>
          </article>
          <span className="step-chevron" aria-hidden="true">
            <Icon icon={ChevronRight} size={20} />
          </span>
          <article className="step-card">
            <div className="step-card-top">
              <span className="step-index">3</span>
              <IconTile icon={UserCheck} accent="education" />
            </div>
            <h3>Send a request</h3>
            <p className="muted">Pick a day. You and the provider agree the time on a call.</p>
          </article>
          <span className="step-chevron" aria-hidden="true">
            <Icon icon={ChevronRight} size={20} />
          </span>
          <article className="step-card">
            <div className="step-card-top">
              <span className="step-index">4</span>
              <IconTile icon={Star} accent="electrical" />
            </div>
            <h3>Complete and review</h3>
            <p className="muted">Finish the service and leave a review.</p>
          </article>
        </div>
      </HomeSection>

      <HomeSection
        title="Why choose Khidma?"
        subtitle="A better way to book local services."
      >
        <div className="why-grid">
          <article className="why-card">
            <IconTile icon={BadgeCheck} accent="home" />
            <div>
              <h3>Verified providers</h3>
              <p className="muted">
                Work with approved professionals who serve your city.
              </p>
            </div>
          </article>
          <article className="why-card">
            <IconTile icon={Scale} accent="technology" />
            <div>
              <h3>Clear pricing</h3>
              <p className="muted">
                The provider quotes a price when they accept the visit.
              </p>
            </div>
          </article>
          <article className="why-card">
            <IconTile icon={CalendarCheck} accent="cleaning" />
            <div>
              <h3>Easy booking management</h3>
              <p className="muted">
                Track your bookings and keep every step in one place.
              </p>
            </div>
          </article>
        </div>
      </HomeSection>

      <HomeSection
        title="Built for a clearer service experience"
        subtitle="A structured path for customers and providers."
      >
        <div className="confidence-grid">
          <article className="confidence-card">
            <IconTile icon={Users} accent="home" />
            <div>
              <h3>Customers</h3>
              <p className="muted">Browse services, pick a day, and keep your history in your profile.</p>
            </div>
          </article>
          <article className="confidence-card">
            <IconTile icon={Briefcase} accent="technology" />
            <div>
              <h3>Providers</h3>
              <p className="muted">
                Find relevant opportunities and manage work.
              </p>
            </div>
          </article>
          <article className="confidence-card">
            <IconTile icon={Workflow} accent="education" />
            <div>
              <h3>Controlled workflow</h3>
              <p className="muted">
                Every step has clear permissions and states.
              </p>
            </div>
          </article>
        </div>
      </HomeSection>

      <div className="container">
        <section className="cta-band">
          <span className="cta-deco cta-deco-one" aria-hidden="true" />
          <span className="cta-deco cta-deco-two" aria-hidden="true" />
          <div className="cta-copy">
            <span className="eyebrow">Ready to get started?</span>
            <h2>Find trusted professionals near you.</h2>
            <p>Get the help you need, when you need it.</p>
          </div>
          <Button variant="light" to="/catalog" iconRight={ArrowRight}>
            Find a Service
          </Button>
        </section>
      </div>
    </div>
  )
}

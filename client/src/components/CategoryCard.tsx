import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Icon, categoryVisual } from './icons'

export function CategoryCard({
  name,
  to,
}: {
  name: string
  serviceCount: number
  to?: string
}) {
  const visual = categoryVisual(name)
  const description = visual.description

  const content = (
    <>
      <div className="category-card-top">
        <span className={`category-card-icon icon-tile-${visual.accent}`}>
          <Icon icon={visual.icon} size={20} />
        </span>
        <span className="category-card-arrow" aria-hidden="true">
          <Icon icon={ArrowRight} size={16} />
        </span>
      </div>
      <h3>{name}</h3>
      <p className="count">{description}</p>
    </>
  )

  if (to) {
    return (
      <Link className={`category-card category-card-${visual.accent}`} to={to}>
        {content}
      </Link>
    )
  }

  return (
    <article className={`category-card category-card-${visual.accent}`}>
      {content}
    </article>
  )
}

import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Icon, IconTile, categoryVisual } from './icons'

export function CategoryCard({
  name,
  serviceCount,
  to,
}: {
  name: string
  serviceCount: number
  to?: string
}) {
  const visual = categoryVisual(name)
  const content = (
    <>
      <div className="category-card-top">
        <IconTile icon={visual.icon} accent={visual.accent} />
        <Icon icon={ArrowRight} size={16} className="card-arrow" />
      </div>
      <h3>{name}</h3>
      <p className="count">
        {serviceCount} {serviceCount === 1 ? 'service' : 'services'}
      </p>
    </>
  )

  if (to) {
    return (
      <Link className="category-card" to={to}>
        {content}
      </Link>
    )
  }

  return <article className="category-card">{content}</article>
}

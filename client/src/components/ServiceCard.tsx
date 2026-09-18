import { ArrowRight, Clock3 } from 'lucide-react'
import { Icon } from './icons'
import { getCategoryBadgeTone } from '../utils/catalogVisuals'
import { getServiceVisual } from '../utils/serviceVisuals'

export function ServiceCard({
  name,
  categoryName,
}: {
  name: string
  categoryName: string
}) {
  const visual = getServiceVisual(name)
  const tone = getCategoryBadgeTone(categoryName)

  return (
    <article className="catalog-card">
      <div className="catalog-card-media">
        <img src={visual.image} alt={visual.alt} loading="lazy" />
      </div>
      <div className="catalog-card-body">
        <span className={`catalog-badge catalog-badge-${tone}`}>{categoryName}</span>
        <h3>{name}</h3>
        <p>{visual.description}</p>
        <div className="catalog-card-foot">
          <span className="catalog-card-status">
            <Icon icon={Clock3} size={16} />
            Coming in next phase
          </span>
          <span className="catalog-card-arrow" aria-hidden="true">
            <Icon icon={ArrowRight} size={16} />
          </span>
        </div>
      </div>
    </article>
  )
}

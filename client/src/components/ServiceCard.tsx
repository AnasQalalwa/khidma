import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Icon } from './icons'
import { serviceImageUrl } from '../api/catalog'
import { getCategoryBadgeTone } from '../utils/catalogVisuals'
import { getServiceVisual } from '../utils/serviceVisuals'

export function ServiceCard({
  id,
  name,
  description,
  categoryName,
  hasImage,
}: {
  id: number
  name: string
  description: string
  categoryName: string
  hasImage: boolean
}) {
  const visual = getServiceVisual(name)
  const tone = getCategoryBadgeTone(categoryName)
  const copy = description.trim() || visual.description

  return (
    <Link to={`/catalog/services/${id}`} className="catalog-card">
      <div className="catalog-card-media">
        <img
          src={hasImage ? serviceImageUrl(id) : visual.image}
          alt={hasImage ? `${name} service` : visual.alt}
          loading="lazy"
        />
      </div>
      <div className="catalog-card-body">
        <span className={`catalog-badge catalog-badge-${tone}`}>{categoryName}</span>
        <h3>{name}</h3>
        <p>{copy}</p>
        <div className="catalog-card-foot">
          <span className="catalog-card-status">View providers</span>
          <span className="catalog-card-arrow" aria-hidden="true">
            <Icon icon={ArrowRight} size={16} />
          </span>
        </div>
      </div>
    </Link>
  )
}

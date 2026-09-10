import { ArrowRight } from 'lucide-react'
import { Icon, IconTile, categoryVisual } from './icons'

export function ServiceCard({
  name,
  categoryName,
}: {
  name: string
  categoryName: string
}) {
  const visual = categoryVisual(name)

  return (
    <article className="service-card">
      <div className="service-card-top">
        <IconTile icon={visual.icon} accent={visual.accent} />
        <Icon icon={ArrowRight} size={16} className="card-arrow" />
      </div>
      <p className="meta">{categoryName}</p>
      <h3>{name}</h3>
      <p className="coming-soon">Coming in next phase</p>
    </article>
  )
}

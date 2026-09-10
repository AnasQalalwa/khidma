import type { LucideIcon } from 'lucide-react'
import { IconTile } from './icons'

export function StatCard({
  title,
  hint,
  icon,
  accent = 'service',
}: {
  title: string
  hint: string
  icon: LucideIcon
  accent?: 'service' | 'home' | 'technology' | 'education' | 'cleaning'
}) {
  return (
    <article className="stat-card">
      <IconTile icon={icon} accent={accent} />
      <p className="meta">{title}</p>
      <p className="value">--</p>
      <p className="coming-soon">{hint}</p>
    </article>
  )
}

import type { LucideIcon } from 'lucide-react'
import { IconTile } from './icons'

export function StatCard({
  title,
  value,
  hint,
  icon,
  accent = 'service',
}: {
  title: string
  value: string | number
  hint?: string
  icon: LucideIcon
  accent?: 'service' | 'home' | 'technology' | 'education' | 'cleaning'
}) {
  return (
    <article className="stat-card">
      <IconTile icon={icon} accent={accent} />
      <p className="meta">{title}</p>
      <p className="value">{value}</p>
      {hint ? <p className="coming-soon">{hint}</p> : null}
    </article>
  )
}

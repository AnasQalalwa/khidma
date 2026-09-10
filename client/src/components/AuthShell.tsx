import type { ReactNode } from 'react'
import { CheckCircle2, ShieldCheck, Sparkles } from 'lucide-react'
import { Icon } from './icons'

export function AuthShell({
  eyebrow,
  title,
  description,
  points,
  children,
}: {
  eyebrow: string
  title: string
  description: string
  points?: string[]
  children: ReactNode
}) {
  const icons = [ShieldCheck, CheckCircle2, Sparkles]

  return (
    <div className="auth-shell">
      <aside className="auth-visual">
        <span className="eyebrow">{eyebrow}</span>
        <h2>{title}</h2>
        <p>{description}</p>
        {points && points.length > 0 ? (
          <div className="auth-points">
            {points.map((point, index) => {
              const PointIcon = icons[index] ?? CheckCircle2
              return (
                <div className="auth-point" key={point}>
                  <Icon icon={PointIcon} size={16} />
                  <span>{point}</span>
                </div>
              )
            })}
          </div>
        ) : null}
      </aside>
      <div className="auth-panel">
        <section className="auth-card">{children}</section>
      </div>
    </div>
  )
}

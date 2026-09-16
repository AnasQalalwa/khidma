import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { AuthVisual } from './AuthVisual'
import { Icon } from './icons'

export type AuthBenefit = {
  icon: LucideIcon
  title: string
  description: string
}

export function AuthShell({
  eyebrow,
  title,
  description,
  benefits,
  variant = 'login',
  children,
}: {
  eyebrow: string
  title: ReactNode
  description: string
  benefits: AuthBenefit[]
  variant?: 'login' | 'register'
  children: ReactNode
}) {
  return (
    <div className={`auth-shell auth-shell-${variant}`}>
      <aside className="auth-visual">
        <div className="auth-copy">
          <span className="eyebrow">{eyebrow}</span>
          <h2 className="auth-title">{title}</h2>
          <p className="auth-lead">{description}</p>
          <ul className="auth-points">
            {benefits.map((benefit) => (
              <li className="auth-point" key={benefit.title}>
                <span className="auth-point-icon">
                  <Icon icon={benefit.icon} size={16} />
                </span>
                <span>
                  <strong>{benefit.title}</strong>
                  <span>{benefit.description}</span>
                </span>
              </li>
            ))}
          </ul>
          <p className="auth-tagline">
            Local services.
            <br />
            Stronger communities.
          </p>
        </div>
        <AuthVisual />
      </aside>
      <div className="auth-panel">
        <section className={`auth-card auth-card-${variant}`}>{children}</section>
      </div>
    </div>
  )
}

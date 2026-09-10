import type { ButtonHTMLAttributes, ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Loader2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Icon } from './icons'

type ButtonProps = {
  children: ReactNode
  variant?: 'primary' | 'secondary' | 'ghost'
  size?: 'md' | 'sm'
  to?: string
  block?: boolean
  className?: string
  icon?: LucideIcon
  iconRight?: LucideIcon
  loading?: boolean
} & ButtonHTMLAttributes<HTMLButtonElement>

function classNames(props: Pick<ButtonProps, 'variant' | 'size' | 'block' | 'className'>) {
  return [
    'btn',
    `btn-${props.variant ?? 'primary'}`,
    props.size === 'sm' ? 'btn-sm' : '',
    props.block ? 'btn-block' : '',
    props.className ?? '',
  ]
    .filter(Boolean)
    .join(' ')
}

export function Button({
  children,
  variant = 'primary',
  size = 'md',
  to,
  block = false,
  className = '',
  type = 'button',
  icon,
  iconRight,
  loading = false,
  disabled,
  ...props
}: ButtonProps) {
  const classes = classNames({ variant, size, block, className })
  const content = (
    <>
      {loading ? (
        <Icon icon={Loader2} size={16} className="spin" />
      ) : icon ? (
        <Icon icon={icon} size={16} />
      ) : null}
      {children}
      {!loading && iconRight ? <Icon icon={iconRight} size={16} /> : null}
    </>
  )

  if (to) {
    return (
      <Link className={classes} to={to}>
        {content}
      </Link>
    )
  }

  return (
    <button
      className={classes}
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {content}
    </button>
  )
}

export function IconButton({
  label,
  icon,
  variant = 'ghost',
  className = '',
  type = 'button',
  ...props
}: {
  label: string
  icon: LucideIcon
  variant?: 'ghost' | 'secondary'
  className?: string
} & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type={type}
      className={`icon-btn icon-btn-${variant} ${className}`.trim()}
      aria-label={label}
      {...props}
    >
      <Icon icon={icon} size={20} />
    </button>
  )
}

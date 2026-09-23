import type { MouseEventHandler } from 'react'
import { NavLink } from 'react-router-dom'
import logo from '../assets/brand/khidma-logo.png'
import mark from '../assets/brand/khidma-mark.png'

export function BrandLink({
  variant = 'onLight',
  size = 'compact',
  onClick,
}: {
  variant?: 'onLight' | 'onDark'
  size?: 'compact' | 'full'
  onClick?: MouseEventHandler<HTMLAnchorElement>
}) {
  const compact = size === 'compact'

  return (
    <NavLink
      to="/"
      className={[
        'brand',
        compact ? '' : 'brand-full',
        variant === 'onDark' ? 'brand-on-dark' : '',
      ]
        .filter(Boolean)
        .join(' ')}
      aria-label="Khidma home"
      onClick={onClick}
    >
      <span className="brand-tile">
        <img className="brand-logo" src={logo} alt="Khidma" />
        {compact ? <img className="brand-mark-img" src={mark} alt="" /> : null}
      </span>
    </NavLink>
  )
}

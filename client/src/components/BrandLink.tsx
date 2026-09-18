import type { MouseEventHandler } from 'react'
import { NavLink } from 'react-router-dom'
import compactLogo from '../assets/brand/khidma-logo-compact.svg'
import compactLogoWhite from '../assets/brand/khidma-logo-compact-white.svg'
import logo from '../assets/brand/khidma-logo.svg'
import logoWhite from '../assets/brand/khidma-logo-white.svg'
import mark from '../assets/brand/khidma-mark.svg'

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
  const wordmark = variant === 'onDark'
    ? compact
      ? compactLogoWhite
      : logoWhite
    : compact
      ? compactLogo
      : logo
  const height = compact ? 32 : 48

  return (
    <NavLink
      to="/"
      className={compact ? 'brand' : 'brand brand-full'}
      aria-label="Khidma home"
      onClick={onClick}
    >
      <img className="brand-logo" src={wordmark} alt="Khidma" height={height} />
      {compact ? <img className="brand-mark-img" src={mark} alt="Khidma" height={32} /> : null}
    </NavLink>
  )
}

import type { MouseEventHandler } from 'react'
import { NavLink } from 'react-router-dom'
import logo from '../assets/brand/khidma-logo.svg'
import logoWhite from '../assets/brand/khidma-logo-white.svg'
import mark from '../assets/brand/khidma-mark.svg'

export function BrandLink({
  variant = 'onLight',
  onClick,
}: {
  variant?: 'onLight' | 'onDark'
  onClick?: MouseEventHandler<HTMLAnchorElement>
}) {
  const wordmark = variant === 'onDark' ? logoWhite : logo

  return (
    <NavLink to="/" className="brand" aria-label="Khidma home" onClick={onClick}>
      <img className="brand-logo" src={wordmark} alt="Khidma" height={36} />
      <img className="brand-mark-img" src={mark} alt="Khidma" height={32} />
    </NavLink>
  )
}

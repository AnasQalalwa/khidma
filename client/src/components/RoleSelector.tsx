import { Briefcase, User } from 'lucide-react'
import { IconTile } from './icons'

export type PublicRole = 'Customer' | 'Provider'

export function RoleSelector({
  value,
  onChange,
  error,
}: {
  value: PublicRole
  onChange: (role: PublicRole) => void
  error?: string
}) {
  return (
    <div className="field">
      <span id="account-type-label">Account type</span>
      <div
        className="role-grid"
        role="radiogroup"
        aria-labelledby="account-type-label"
      >
        <button
          type="button"
          className="role-choice"
          role="radio"
          aria-checked={value === 'Customer'}
          onClick={() => onChange('Customer')}
        >
          <IconTile icon={User} accent="home" />
          <strong>Customer</strong>
          <span>I need services</span>
        </button>
        <button
          type="button"
          className="role-choice"
          role="radio"
          aria-checked={value === 'Provider'}
          onClick={() => onChange('Provider')}
        >
          <IconTile icon={Briefcase} accent="technology" />
          <strong>Provider</strong>
          <span>I offer professional services</span>
        </button>
      </div>
      {error ? <span className="field-error">{error}</span> : null}
    </div>
  )
}

import { useId, type KeyboardEvent } from 'react'
import { BriefcaseBusiness, UserRound } from 'lucide-react'
import { Icon } from './icons'

export type PublicRole = 'Customer' | 'Provider'

const roles: Array<{
  value: PublicRole
  title: string
  subtitle: string
  icon: typeof UserRound
}> = [
  {
    value: 'Customer',
    title: 'Customer',
    subtitle: 'I need services',
    icon: UserRound,
  },
  {
    value: 'Provider',
    title: 'Provider',
    subtitle: 'I offer professional services',
    icon: BriefcaseBusiness,
  },
]

export function RoleSelector({
  value,
  onChange,
  error,
}: {
  value: PublicRole
  onChange: (role: PublicRole) => void
  error?: string
}) {
  const labelId = useId()
  const errorId = `${labelId}-error`

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      event.preventDefault()
      onChange('Provider')
    }

    if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      event.preventDefault()
      onChange('Customer')
    }

    if (event.key === 'Home') {
      event.preventDefault()
      onChange('Customer')
    }

    if (event.key === 'End') {
      event.preventDefault()
      onChange('Provider')
    }
  }

  return (
    <div className="field">
      <span id={labelId}>Account type</span>
      <div
        className="role-grid"
        role="radiogroup"
        aria-labelledby={labelId}
        aria-describedby={error ? errorId : undefined}
        onKeyDown={handleKeyDown}
      >
        {roles.map((role) => {
          const selected = value === role.value

          return (
            <button
              key={role.value}
              type="button"
              className={selected ? 'role-choice is-selected' : 'role-choice'}
              role="radio"
              aria-checked={selected}
              onClick={() => onChange(role.value)}
            >
              <span className="role-choice-icon">
                <Icon icon={role.icon} size={20} />
              </span>
              <span className="role-indicator" aria-hidden="true" />
              <strong>{role.title}</strong>
              <span className="role-choice-copy">{role.subtitle}</span>
            </button>
          )
        })}
      </div>
      {error ? (
        <span id={errorId} className="field-error">
          {error}
        </span>
      ) : null}
    </div>
  )
}

import { useId, useState } from 'react'
import { Eye, EyeOff, Lock } from 'lucide-react'
import { Icon } from './icons'

export function PasswordField({
  id,
  label,
  value,
  onChange,
  autoComplete,
  error,
  required,
}: {
  id?: string
  label: string
  value: string
  onChange: (value: string) => void
  autoComplete?: string
  error?: string
  required?: boolean
}) {
  const generatedId = useId()
  const fieldId = id ?? generatedId
  const errorId = `${fieldId}-error`
  const [visible, setVisible] = useState(false)

  return (
    <div className="field">
      <label htmlFor={fieldId}>{label}</label>
      <div className="field-control has-icon password-field">
        <Icon icon={Lock} size={16} className="field-icon" />
        <input
          id={fieldId}
          type={visible ? 'text' : 'password'}
          autoComplete={autoComplete}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          required={required}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
        />
        <button
          type="button"
          className="password-toggle"
          aria-label={visible ? 'Hide password' : 'Show password'}
          onClick={() => setVisible((current) => !current)}
        >
          <Icon icon={visible ? EyeOff : Eye} size={16} />
        </button>
      </div>
      {error ? (
        <p id={errorId} className="field-error">
          {error}
        </p>
      ) : null}
    </div>
  )
}

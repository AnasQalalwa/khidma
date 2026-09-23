import { useId, useState } from 'react'
import { Check, Eye, EyeOff, LockKeyhole } from 'lucide-react'
import { Icon } from './icons'
import { PASSWORD_RULES, passwordRuleState } from '../utils/validation'

export function PasswordField({
  id,
  label,
  value,
  onChange,
  autoComplete,
  error,
  required,
  placeholder,
  showRules = false,
}: {
  id?: string
  label: string
  value: string
  onChange: (value: string) => void
  autoComplete?: string
  error?: string
  required?: boolean
  placeholder?: string
  showRules?: boolean
}) {
  const generatedId = useId()
  const fieldId = id ?? generatedId
  const errorId = `${fieldId}-error`
  const rulesId = `${fieldId}-rules`
  const [visible, setVisible] = useState(false)
  const rules = passwordRuleState(value)
  const describedBy = [error ? errorId : null, showRules ? rulesId : null]
    .filter(Boolean)
    .join(' ')

  return (
    <div className="field">
      <label htmlFor={fieldId}>{label}</label>
      <div className="field-control has-icon password-field">
        <Icon icon={LockKeyhole} size={16} className="field-icon" />
        <input
          id={fieldId}
          type={visible ? 'text' : 'password'}
          autoComplete={autoComplete}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          required={required}
          placeholder={placeholder}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy || undefined}
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
      {showRules ? (
        <ul id={rulesId} className="password-rules">
          {PASSWORD_RULES.map((rule) => {
            const met = rules[rule.id]
            return (
              <li
                key={rule.id}
                className={met ? 'password-rule is-met' : 'password-rule'}
              >
                <span className="password-rule-mark" aria-hidden="true">
                  {met ? <Icon icon={Check} size={16} /> : null}
                </span>
                <span>{rule.label}</span>
              </li>
            )
          })}
        </ul>
      ) : null}
      {error ? (
        <p id={errorId} className="field-error">
          {error}
        </p>
      ) : null}
    </div>
  )
}

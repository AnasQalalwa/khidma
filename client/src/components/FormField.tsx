import {
  cloneElement,
  useId,
  type InputHTMLAttributes,
  type ReactElement,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react'
import type { LucideIcon } from 'lucide-react'
import { Icon } from './icons'

type ControlElement = ReactElement<
  | InputHTMLAttributes<HTMLInputElement>
  | TextareaHTMLAttributes<HTMLTextAreaElement>
  | SelectHTMLAttributes<HTMLSelectElement>
>

export function FormField({
  id,
  label,
  error,
  hint,
  icon,
  children,
}: {
  id?: string
  label: string
  error?: string
  hint?: string
  icon?: LucideIcon
  children: ControlElement
}) {
  const generatedId = useId()
  const fieldId = id ?? generatedId
  const errorId = `${fieldId}-error`
  const hintId = `${fieldId}-hint`
  const describedBy = [error ? errorId : null, hint ? hintId : null]
    .filter(Boolean)
    .join(' ')

  const control = cloneElement(children, {
    id: fieldId,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': describedBy || undefined,
  })

  return (
    <div className="field">
      <label htmlFor={fieldId}>{label}</label>
      <div className={icon ? 'field-control has-icon' : 'field-control'}>
        {icon ? <Icon icon={icon} size={16} className="field-icon" /> : null}
        {control}
      </div>
      {hint ? (
        <p id={hintId} className="field-hint">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} className="field-error">
          {error}
        </p>
      ) : null}
    </div>
  )
}

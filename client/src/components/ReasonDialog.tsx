import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Button } from './Button'

export function ReasonDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  label = 'Reason',
  danger = false,
  busy = false,
  onConfirm,
  onClose,
}: {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  label?: string
  danger?: boolean
  busy?: boolean
  onConfirm: (reason: string) => void
  onClose: () => void
}) {
  const titleId = useId()
  const descriptionId = useId()
  const errorId = useId()
  const fieldId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) {
      setReason('')
      setError(null)
      return
    }

    const previouslyFocused = document.activeElement
    inputRef.current?.focus()

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !busy) {
        onClose()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    document.body.classList.add('nav-locked')

    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.classList.remove('nav-locked')
      if (previouslyFocused instanceof HTMLElement) {
        previouslyFocused.focus()
      }
    }
  }, [busy, onClose, open])

  if (!open) {
    return null
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (busy) {
      return
    }

    const value = reason.trim()
    if (!value) {
      setError('A reason is required.')
      inputRef.current?.focus()
      return
    }

    onConfirm(value)
  }

  return (
    <div className="dialog-backdrop" onClick={() => (busy ? undefined : onClose())}>
      <form
        ref={dialogRef as never}
        className="dialog-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        onClick={(event) => event.stopPropagation()}
        onSubmit={handleSubmit}
        noValidate
      >
        <h2 id={titleId}>{title}</h2>
        <p id={descriptionId} className="muted">
          {description}
        </p>
        <div className="field">
          <label htmlFor={fieldId}>{label}</label>
          <textarea
            id={fieldId}
            ref={inputRef}
            rows={4}
            value={reason}
            onChange={(event) => {
              setReason(event.target.value)
              if (error) {
                setError(null)
              }
            }}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            required
          />
          {error ? (
            <p id={errorId} className="field-error" role="alert">
              {error}
            </p>
          ) : null}
        </div>
        <div className="dialog-actions">
          <Button variant="secondary" type="button" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button type="submit" loading={busy} className={danger ? 'btn-danger' : undefined}>
            {confirmLabel}
          </Button>
        </div>
      </form>
    </div>
  )
}

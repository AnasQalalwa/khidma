import { useEffect, useId, useRef, type ReactNode } from 'react'
import { Button } from './Button'

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  danger = false,
  busy = false,
  onConfirm,
  onClose,
  children,
}: {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  danger?: boolean
  busy?: boolean
  onConfirm: () => void
  onClose: () => void
  children?: ReactNode
}) {
  const titleId = useId()
  const descriptionId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }

    const previouslyFocused = document.activeElement
    dialogRef.current?.focus()

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

  return (
    <div className="dialog-backdrop" onClick={() => (busy ? undefined : onClose())}>
      <div
        ref={dialogRef}
        className="dialog-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId}>{title}</h2>
        <p id={descriptionId} className="muted">
          {description}
        </p>
        {children}
        <div className="dialog-actions">
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button
            onClick={onConfirm}
            loading={busy}
            className={danger ? 'btn-danger' : undefined}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  )
}

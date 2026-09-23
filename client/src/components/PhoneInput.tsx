import { useEffect, useId, useRef, useState, type InputHTMLAttributes } from 'react'
import { CALLING_CODES, DEFAULT_DIAL_CODE } from '../data/callingCodes'
import { formatPhone, splitPhone } from '../utils/validation'

type PhoneInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange' | 'type'> & {
  value: string
  onChange: (value: string) => void
}

export function PhoneInput({
  value,
  onChange,
  autoComplete = 'tel',
  ...rest
}: PhoneInputProps) {
  const parsed = splitPhone(value)
  const [dial, setDial] = useState(parsed.dial || DEFAULT_DIAL_CODE)
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const listId = useId()
  const selected = CALLING_CODES.find((code) => code.dial === dial) ?? CALLING_CODES[0]

  useEffect(() => {
    if (value.trim()) {
      setDial(splitPhone(value).dial)
    }
  }, [value])

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  function choose(nextDial: string) {
    setDial(nextDial)
    setOpen(false)
    onChange(formatPhone(parsed.local, nextDial))
  }

  return (
    <div className="phone-field" ref={rootRef}>
      <div className="phone-code">
        <button
          type="button"
          className="phone-code-trigger"
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={listId}
          aria-label={`Country code, ${selected.name}`}
          onClick={() => setOpen((current) => !current)}
        >
          <span>{selected.dial}</span>
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="m6 9 6 6 6-6" />
          </svg>
        </button>
        {open ? (
          <ul id={listId} className="phone-code-menu" role="listbox" aria-label="Country code">
            {CALLING_CODES.map((code) => (
              <li key={code.dial}>
                <button
                  type="button"
                  role="option"
                  aria-selected={code.dial === selected.dial}
                  onClick={() => choose(code.dial)}
                >
                  <span>{code.name}</span>
                  <span>{code.dial}</span>
                </button>
              </li>
            ))}
          </ul>
        ) : null}
      </div>
      <input
        {...rest}
        type="tel"
        inputMode="numeric"
        autoComplete={autoComplete}
        placeholder={selected.dial === DEFAULT_DIAL_CODE ? '0598969367' : 'Local number'}
        maxLength={selected.dial === DEFAULT_DIAL_CODE ? 10 : 14}
        value={parsed.local}
        onChange={(event) => onChange(formatPhone(event.target.value, dial))}
      />
    </div>
  )
}

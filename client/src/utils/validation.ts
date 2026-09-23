import { CALLING_CODES, CALLING_CODES_BY_LENGTH, DEFAULT_DIAL_CODE } from '../data/callingCodes'

export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$/

export const PHONE_REQUIREMENT_MESSAGE =
  'Choose a country code and enter the local number, such as +970 0598969367.'

export const PASSWORD_REQUIREMENT_MESSAGE =
  'Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number, and a special character.'

export const PASSWORD_RULES = [
  {
    id: 'length',
    label: 'At least 8 characters',
    test: (value: string) => value.length >= 8,
  },
  {
    id: 'lower',
    label: 'One lowercase letter (a–z)',
    test: (value: string) => /[a-z]/.test(value),
  },
  {
    id: 'upper',
    label: 'One uppercase letter (A–Z)',
    test: (value: string) => /[A-Z]/.test(value),
  },
  {
    id: 'digit',
    label: 'One number (0–9)',
    test: (value: string) => /\d/.test(value),
  },
  {
    id: 'special',
    label: 'One special character (!@#$…)',
    test: (value: string) => /[^A-Za-z0-9]/.test(value),
  },
] as const

export type PasswordRuleId = (typeof PASSWORD_RULES)[number]['id']

export function isValidEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value.trim())
}

export function splitPhone(value: string): { dial: string; local: string } {
  const compact = value.trim().replace(/[\s\-()]/g, '')
  if (compact.startsWith('+')) {
    const match = CALLING_CODES_BY_LENGTH.find((code) => compact.startsWith(code.dial))
    if (match) {
      return {
        dial: match.dial,
        local: compact.slice(match.dial.length).replace(/\D/g, '').slice(0, 14),
      }
    }
  }

  return {
    dial: DEFAULT_DIAL_CODE,
    local: compact.replace(/\D/g, '').slice(0, 14),
  }
}

export function formatPhone(value: string, dial?: string): string {
  const parsed = splitPhone(value)
  const code = dial ?? parsed.dial
  const local = parsed.local.slice(0, code === DEFAULT_DIAL_CODE ? 10 : 14)
  return local ? `${code} ${local}` : ''
}

export function isValidPhone(value: string): boolean {
  const { dial, local } = splitPhone(value)
  if (!CALLING_CODES.some((code) => code.dial === dial)) {
    return false
  }

  if (dial === DEFAULT_DIAL_CODE) {
    return /^0\d{8,9}$/.test(local)
  }

  return /^\d{6,14}$/.test(local)
}

export function displayPhone(value: string): string {
  return isValidPhone(value) ? formatPhone(value) : value.trim()
}

export function passwordRuleState(value: string): Record<PasswordRuleId, boolean> {
  return Object.fromEntries(
    PASSWORD_RULES.map((rule) => [rule.id, rule.test(value)]),
  ) as Record<PasswordRuleId, boolean>
}

export function isStrongPassword(value: string): boolean {
  return PASSWORD_RULES.every((rule) => rule.test(value))
}

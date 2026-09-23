import { describe, expect, it } from 'vitest'
import { isStrongPassword, isValidEmail } from './validation'

describe('isValidEmail', () => {
  it('accepts a normal email address', () => {
    expect(isValidEmail('you@example.com')).toBe(true)
    expect(isValidEmail('anas@gmail.com')).toBe(true)
  })

  it('rejects values that are not emails', () => {
    expect(isValidEmail('not-an-email')).toBe(false)
    expect(isValidEmail('anas@gmail')).toBe(false)
    expect(isValidEmail('anas@gmail.')).toBe(false)
    expect(isValidEmail('')).toBe(false)
  })
})

describe('isStrongPassword', () => {
  it('accepts a password that meets every rule', () => {
    expect(isStrongPassword('ValidPass1!')).toBe(true)
  })

  it('rejects passwords that miss length or character classes', () => {
    expect(isStrongPassword('xxxxx')).toBe(false)
    expect(isStrongPassword('password')).toBe(false)
    expect(isStrongPassword('Password1')).toBe(false)
    expect(isStrongPassword('PASSWORD1!')).toBe(false)
  })
})

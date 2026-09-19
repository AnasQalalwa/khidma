import { describe, expect, it } from 'vitest'
import { formatCount, formatDelta, formatMoney, formatPercent } from './format'

describe('formatDelta', () => {
  it('formats a percent change with a sign', () => {
    expect(formatDelta(0.15, 'percent')).toBe(`+${formatPercent(0.15)}`)
    expect(formatDelta(-0.04, 'percent')).toBe(`−${formatPercent(0.04)}`)
  })

  it('formats a currency change with a sign', () => {
    expect(formatDelta(75, 'currency')).toBe(`+${formatMoney(75)}`)
    expect(formatDelta(-12.5, 'currency')).toBe(`−${formatMoney(12.5)}`)
  })

  it('formats a count change with a sign', () => {
    expect(formatDelta(4, 'count')).toBe(`+${formatCount(4)}`)
    expect(formatDelta(-6, 'count')).toBe(`−${formatCount(6)}`)
  })

  it('formats a rate change in percentage points', () => {
    expect(formatDelta(0.8, 'pts')).toBe('+80 pts')
    expect(formatDelta(0.15, 'pts')).toBe('+15 pts')
    expect(formatDelta(-0.05, 'pts')).toBe('−5 pts')
  })
})

import { describe, expect, it } from 'vitest'
import { cityChoices, nearestCity } from './cities'

describe('cityChoices', () => {
  it('lists Palestinian cities and keeps an unknown current value', () => {
    expect(cityChoices()).toContain('Ramallah')
    expect(cityChoices()).toContain('Gaza')
    expect(cityChoices('CustomTown')[0]).toBe('CustomTown')
  })
})

describe('nearestCity', () => {
  it('maps coordinates near Ramallah to Ramallah', () => {
    expect(nearestCity(31.9, 35.2).name).toBe('Ramallah')
  })

  it('maps coordinates near Gaza to Gaza', () => {
    expect(nearestCity(31.5, 34.47).name).toBe('Gaza')
  })
})

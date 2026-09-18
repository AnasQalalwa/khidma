import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DeltaChip } from './DeltaChip'

describe('DeltaChip', () => {
  it('uses up tone and text when the value increases', () => {
    render(<DeltaChip value={12} previous={8} kind="count" />)
    expect(screen.getByText('+4')).toBeInTheDocument()
    expect(screen.getByText(/increased/i)).toBeInTheDocument()
  })

  it('uses down tone and text when the value decreases', () => {
    render(<DeltaChip value={3} previous={9} kind="count" />)
    expect(screen.getByText('−6')).toBeInTheDocument()
    expect(screen.getByText(/decreased/i)).toBeInTheDocument()
  })

  it('uses a neutral chip when there is no previous value', () => {
    render(<DeltaChip value={4} previous={null} kind="count" />)
    expect(screen.getByText('No comparison')).toBeInTheDocument()
    expect(screen.getByText(/unchanged/i)).toBeInTheDocument()
  })
})

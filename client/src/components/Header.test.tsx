import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthContext } from '../auth/AuthContext'
import { createAuthValue } from '../test/render'
import { Header } from './Header'

describe('Header', () => {
  it('renders the Khidma home brand link with logo and mark', () => {
    render(
      <MemoryRouter>
        <AuthContext.Provider value={createAuthValue()}>
          <Header />
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    const home = screen.getByRole('link', { name: 'Khidma home' })
    expect(home).toHaveAttribute('href', '/')
    expect(screen.getAllByAltText('Khidma')).toHaveLength(2)
    expect(home.querySelector('.brand-logo')).toHaveAttribute('height', '32')
    expect(home.querySelector('.brand-logo')?.getAttribute('src')).toMatch(/khidma-logo-compact/)
    expect(home.querySelector('.brand-mark-img')).toHaveAttribute('height', '32')
  })
})

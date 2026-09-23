import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthContext } from '../auth/AuthContext'
import { createAuthValue, customerUser } from '../test/render'
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
    expect(screen.getAllByAltText('Khidma')).toHaveLength(1)
    expect(home.querySelector('.brand-logo')?.getAttribute('src')).toMatch(/khidma-logo/)
    expect(home.querySelector('.brand-mark-img')?.getAttribute('src')).toMatch(/khidma-mark/)
  })

  it('shows bookings and profile for a customer instead of a dashboard', () => {
    render(
      <MemoryRouter>
        <AuthContext.Provider
          value={createAuthValue({ user: customerUser(), authenticated: true })}
        >
          <Header />
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: 'My Bookings' })).toHaveAttribute(
      'href',
      '/account/bookings',
    )
    expect(screen.getByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/account')
    expect(screen.queryByRole('link', { name: 'Dashboard' })).not.toBeInTheDocument()
  })
})

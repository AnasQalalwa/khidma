import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { RequireRole } from './RequireRole'
import { Roles } from './roles'
import { customerUser, providerUser, renderWithRouter } from '../test/render'

describe('RequireRole', () => {
  it('redirects a signed-in user with the wrong role to forbidden', () => {
    const user = providerUser()
    renderWithRouter(
      <RequireRole role={Roles.Customer}>
        <div>Customer workspace</div>
      </RequireRole>,
      {
        route: '/customer',
        path: '/customer',
        auth: { user, authenticated: true },
      },
    )

    expect(screen.getByText('Access forbidden')).toBeInTheDocument()
    expect(screen.queryByText('Customer workspace')).not.toBeInTheDocument()
  })

  it('redirects an anonymous visitor to login', () => {
    renderWithRouter(
      <RequireRole role={Roles.Customer}>
        <div>Customer workspace</div>
      </RequireRole>,
      {
        route: '/customer',
        path: '/customer',
      },
    )

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('renders children for the matching role', () => {
    const user = customerUser()
    renderWithRouter(
      <RequireRole role={Roles.Customer}>
        <div>Customer workspace</div>
      </RequireRole>,
      {
        route: '/customer',
        path: '/customer',
        auth: { user, authenticated: true },
      },
    )

    expect(screen.getByText('Customer workspace')).toBeInTheDocument()
  })
})

import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { AuthContext } from '../auth/AuthContext'
import { createAuthValue } from '../test/render'
import { LoginPage } from './LoginPage'

describe('LoginPage', () => {
  const login = vi.fn()

  beforeEach(() => {
    login.mockReset()
  })

  it('renders an API login error in an alert', async () => {
    login.mockRejectedValue(
      new ApiError('Invalid email or password.', 401),
    )

    render(
      <MemoryRouter>
        <AuthContext.Provider
          value={createAuthValue({ login })}
        >
          <LoginPage />
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Email'), 'wrong@khidma.test')
    await user.type(screen.getByLabelText('Password'), 'not-the-password')
    await user.click(screen.getByRole('button', { name: 'Login' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Invalid email or password.',
    )
  })
})

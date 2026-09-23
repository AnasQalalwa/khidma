import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthContext } from '../auth/AuthContext'
import { createAuthValue, providerUser } from '../test/render'
import { RegisterPage } from './RegisterPage'

describe('RegisterPage', () => {
  const register = vi.fn()

  beforeEach(() => {
    register.mockReset()
  })

  function renderPage() {
    return render(
      <MemoryRouter initialEntries={['/register']}>
        <AuthContext.Provider value={createAuthValue({ register })}>
          <Routes>
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/provider/profile" element={<div>Provider profile</div>} />
            <Route path="/catalog" element={<div>Catalog page</div>} />
            <Route path="/login" element={<div>Login page</div>} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>,
    )
  }

  it('shows live password rules and a modern email hint', () => {
    renderPage()

    expect(screen.getByText('At least 8 characters')).toBeInTheDocument()
    expect(screen.getByText('One lowercase letter (a–z)')).toBeInTheDocument()
    expect(screen.getByText('One uppercase letter (A–Z)')).toBeInTheDocument()
    expect(screen.getByText('One number (0–9)')).toBeInTheDocument()
    expect(screen.getByText('One special character (!@#$…)')).toBeInTheDocument()
    expect(
      screen.getByText('Use an email like you@example.com.'),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('City').tagName).toBe('SELECT')
  })

  it('blocks submit when the email or password is invalid', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Full name'), 'Anas')
    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.type(screen.getByLabelText('Phone number'), '0591234567')
    await user.type(screen.getByLabelText('Password'), 'xxxxx')
    await user.selectOptions(screen.getByLabelText('City'), 'Ramallah')
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(register).not.toHaveBeenCalled()
    expect(
      screen.getByText('Enter a valid email like you@example.com.'),
    ).toBeInTheDocument()
    expect(
      screen.getByText(
        'Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number, and a special character.',
      ),
    ).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('tells providers where to upload documents for admin review', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('radio', { name: /Provider/i }))

    expect(
      screen.getByText(/Profile → Professional verification/i),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/An admin reviews it before you can take new jobs/i),
    ).toBeInTheDocument()
  })

  it('sends a new provider to the profile verification section', async () => {
    register.mockResolvedValue(providerUser())
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('radio', { name: /Provider/i }))
    await user.type(screen.getByLabelText('Full name'), 'Sami Provider')
    await user.type(screen.getByLabelText('Email'), 'sami@example.com')
    await user.type(screen.getByLabelText('Phone number'), '0591234567')
    await user.type(screen.getByLabelText('Password'), 'ValidPass1!')
    await user.selectOptions(screen.getByLabelText('City'), 'Ramallah')
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('Provider profile')).toBeInTheDocument()
    expect(register).toHaveBeenCalledWith(
      expect.objectContaining({
        email: 'sami@example.com',
        phoneNumber: '+970 0591234567',
        role: 'Provider',
        password: 'ValidPass1!',
        city: 'Ramallah',
      }),
    )
  })

  it('sends the country code the user picks', async () => {
    register.mockResolvedValue(providerUser())
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Full name'), 'Lina Customer')
    await user.type(screen.getByLabelText('Email'), 'lina@example.com')
    await user.click(screen.getByRole('button', { name: /Country code/i }))
    await user.click(screen.getByRole('option', { name: /Jordan/i }))
    await user.type(screen.getByLabelText('Phone number'), '0791234567')
    await user.type(screen.getByLabelText('Password'), 'ValidPass1!')
    await user.selectOptions(screen.getByLabelText('City'), 'Ramallah')
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('Provider profile')).toBeInTheDocument()
    expect(register).toHaveBeenCalledWith(
      expect.objectContaining({
        phoneNumber: '+962 0791234567',
        role: 'Customer',
      }),
    )
  })
})

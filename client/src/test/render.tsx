import type { ReactNode } from 'react'
import { render, type RenderOptions } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import type { CurrentUser } from '../api/auth'
import { AuthContext, type AuthContextValue } from '../auth/AuthContext'

export function createAuthValue(
  overrides: Partial<AuthContextValue> = {},
): AuthContextValue {
  return {
    user: null,
    loading: false,
    authenticated: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    refreshUser: vi.fn(),
    ...overrides,
  }
}

export function customerUser(
  overrides: Partial<CurrentUser> = {},
): CurrentUser {
  return {
    id: 'customer-1',
    email: 'customer@khidma.test',
    fullName: 'Test Customer',
    role: 'Customer',
    ...overrides,
  }
}

export function providerUser(
  overrides: Partial<CurrentUser> = {},
): CurrentUser {
  return {
    id: 'provider-1',
    email: 'provider@khidma.test',
    fullName: 'Test Provider',
    role: 'Provider',
    ...overrides,
  }
}

export function adminUser(
  overrides: Partial<CurrentUser> = {},
): CurrentUser {
  return {
    id: 'admin-1',
    email: 'admin@khidma.test',
    fullName: 'Test Admin',
    role: 'Admin',
    ...overrides,
  }
}

export function renderWithRouter(
  ui: ReactNode,
  options?: {
    route?: string
    path?: string
    auth?: Partial<AuthContextValue>
    renderOptions?: Omit<RenderOptions, 'wrapper'>
  },
) {
  const route = options?.route ?? '/'
  const path = options?.path ?? '*'
  const auth = createAuthValue(options?.auth)

  return {
    auth,
    ...render(
      <MemoryRouter initialEntries={[route]}>
        <AuthContext.Provider value={auth}>
          <Routes>
            <Route path={path} element={ui} />
            <Route path="/login" element={<div>Login page</div>} />
            <Route path="/forbidden" element={<div>Access forbidden</div>} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>,
      options?.renderOptions,
    ),
  }
}

import { createContext } from 'react'
import type { CurrentUser, LoginPayload, RegisterPayload } from '../api/auth'

export type AuthContextValue = {
  user: CurrentUser | null
  loading: boolean
  authenticated: boolean
  login: (payload: LoginPayload) => Promise<CurrentUser>
  register: (payload: RegisterPayload) => Promise<CurrentUser>
  logout: () => Promise<void>
  refreshUser: () => Promise<CurrentUser | null>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

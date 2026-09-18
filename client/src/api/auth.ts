import { apiRequest } from './client'

export type CurrentUser = {
  id: string
  email: string
  fullName: string
  role: string
}

export type RegisterPayload = {
  fullName: string
  email: string
  password: string
  role: 'Customer' | 'Provider'
  city: string
  yearsOfExperience?: number
  bio?: string
}

export type LoginPayload = {
  email: string
  password: string
}

export function register(payload: RegisterPayload): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function login(payload: LoginPayload): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', {
    method: 'POST',
  })
}

export function getMe(): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/me')
}
